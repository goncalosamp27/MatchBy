using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MatchBy.Services.BackgroundJobs;

/// <summary>
/// Service responsible for processing background jobs related to match state management.
/// Handles automatic match status updates, email notifications, and match completion.
/// </summary>
public class JobService(ILogger<JobService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, IMemoryCache memoryCache, IEmailSender emailSender): IJobService
{
    private const string ThreeDayWarningCacheKeyPrefix = "3day_warning_sent_";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromDays(4);
    
    /// <summary>
    /// Processes match states by checking pending matches and updating their status accordingly.
    /// Sends cancellation emails for matches within 1 day, confirmation emails for matches within 3 days,
    /// and marks matches as completed when they have passed their scheduled date/time.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessMatchStatesAsync()
    {
        logger.LogInformation("Starting match state processing...");
        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        
        DateTime now = DateTime.UtcNow;
        var oneDay = TimeSpan.FromDays(1);
        var threeDays = TimeSpan.FromDays(3);
        
        List<Match> pendentMatchesBefore1Day = await dbContext.Matches
            .Include(m => m.Creator)
            .Include(m => m.Participants)
            .Where(m => (m.Status == MatchStatus.Pendent || (m.Status == MatchStatus.Confirmed && m.Participants.Count < m.MinPlayers))
                && m.MatchDateTimeUtc <= now.Add(oneDay))
            .ToListAsync();
        
        foreach (Match match in pendentMatchesBefore1Day)
        {
            if (match.Creator?.Email != null)
            {
                try
                {
                    foreach (ApplicationUser participant in match.Participants.Where(p => p.Id != match.CreatorId))
                    {
                        if (participant.Email != null)
                        {
                            await emailSender.SendMatchCancelledAsync(
                                participant.DisplayName,
                                participant.Email,
                                match.Id,
                                match.Sport,
                                match.MatchDateTimeUtc,
                                match.Creator.DisplayName
                            );
                        }
                    }
                    
                    await emailSender.SendMatchCancelationEmail(match.Creator.Email, match.Creator.DisplayName);
                    logger.LogInformation("Cancellation email sent to creator for match {MatchId}", match.Id);
                    logger.LogInformation("Cancellation email sent for match {MatchId} sent to all users participating", match.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send cancellation email for match {MatchId}", match.Id);
                }
            }
            
            match.Status = MatchStatus.Cancelled;
        }
        
        List<Match> pendentMatchesBefore3Days = await dbContext.Matches
            .Include(m => m.Creator)
            .Where(m => m.Status == MatchStatus.Pendent 
                && m.MatchDateTimeUtc <= now.Add(threeDays)
                && m.MatchDateTimeUtc > now.Add(oneDay))
            .ToListAsync();
            
        foreach (Match match in pendentMatchesBefore3Days)
        {
            string cacheKey = $"{ThreeDayWarningCacheKeyPrefix}{match.Id}";
            
            // Check if email was already sent for this match
            if (memoryCache.TryGetValue(cacheKey, out _))
            {
                continue;
            }

            if (match.Creator?.Email != null)
            {
                try
                {
                    await emailSender.SendMatchConfirmationEmail(match.Creator.Email, match.Creator.DisplayName);
                    logger.LogInformation("Confirmation email sent to creator for match {MatchId}", match.Id);
                    memoryCache.Set(cacheKey, true, CacheExpiration);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send confirmation email for match {MatchId}", match.Id);
                }
            }
        }
        
        List<Match> matchesToFinish = await dbContext.Matches
            .Where(m => m.Status != MatchStatus.Completed 
                && m.Status != MatchStatus.Cancelled
                && m.MatchDateTimeUtc < now)
            .ToListAsync();
        
        foreach (Match match in matchesToFinish)
        {
            match.Status = MatchStatus.Completed;
            logger.LogInformation("Match {MatchId} completed", match.Id);
        }
        
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation(
           "Match state processing completed. {CancelledMatches} matches cancelled, {ConfirmationEmails} confirmation emails sent, {CompletedMatches} matches completed.",
            pendentMatchesBefore1Day.Count,
            pendentMatchesBefore3Days.Count,
            matchesToFinish.Count
        );
    }

    /// <summary>
    /// Executes a fire-and-forget background job.
    /// This method can be used for non-critical background tasks that don't require result tracking.
    /// </summary>
    public void FireAndForgetJob()
    {
        logger.LogInformation("FireAndForgetJob");
    }
}
