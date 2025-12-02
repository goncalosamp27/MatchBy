using MatchBy.Data;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Email;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.BackgroundJobs;

public class MatchReminderJob(
    IEmailSender emailSender,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    ILogger<MatchReminderJob> logger,
    INotificationService notificationService)
    : IMatchReminderJob
{
    public async Task SendRemindersAsync()
    {
        await using ApplicationDbContext db = await contextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;

        await Send3DayRemindersAsync(db, now);
        await Send30MinRemindersAsync(db, now);

        await db.SaveChangesAsync();
    }

    private async Task Send3DayRemindersAsync(ApplicationDbContext db, DateTime now)
    {
        List<Match> matches = await db.Matches
            .Include(m => m.Participants)
            .Where(m =>
                (m.Status == MatchStatus.Pendent || m.Status == MatchStatus.Confirmed) &&
                !m.Reminder3DaysSent &&
                m.MatchDateTimeUtc > now &&
                m.MatchDateTimeUtc <= now.AddDays(3))
            .ToListAsync();

        foreach (Match match in matches)
        {
            foreach (ApplicationUser participant in match.Participants)
            {
                if (string.IsNullOrEmpty(participant.Email))
                {
                    continue;
                }

                try
                {
                    await emailSender.SendMatchReminderAsync(
                        participant.Email,
                        participant.UserName!,
                        match,
                        "3 days");

                    logger.LogInformation("Sent 3-day email to {Email}", participant.Email);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send email to {Email}", participant.Email);
                }

                try
                {
                    var notification = new CreateNotificationDto
                    {
                        SenderUserId = match.CreatorId,
                        ReceiverUserId = participant.Id,
                        Type = NotificationType.MatchReminder,
                        Title = "Match Reminder - 3 days",
                        Message = $"Your match '{match.Description}' is in 3 days!",
                        RelatedEntityId = match.Id,
                        RelatedEntityName = "Match"
                    };

                    await notificationService.SendNotificationAsync(notification);
                    logger.LogInformation("Sent notification to {UserId}", participant.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send notification to {UserId}", participant.Id);
                }
            }

            match.Reminder3DaysSent = true;
            logger.LogInformation("Marked 3-day reminder as sent for match {MatchId}", match.Id);
        }
    }

    private async Task Send30MinRemindersAsync(ApplicationDbContext db, DateTime now)
    {
        List<Match> matches = await db.Matches
            .Include(m => m.Participants)
            .Where(m =>
                m.Status == MatchStatus.Confirmed &&
                !m.Reminder30MinSent &&
                m.MatchDateTimeUtc > now &&
                m.MatchDateTimeUtc <= now.AddMinutes(30))
            .ToListAsync();

        foreach (Match match in matches)
        {
            foreach (ApplicationUser participant in match.Participants)
            {
                if (string.IsNullOrEmpty(participant.Email))
                {
                    continue;
                }

                try
                {
                    await emailSender.SendMatchReminderAsync(
                        participant.Email,
                        participant.UserName!,
                        match,
                        "30 minutes");

                    logger.LogInformation("Sent 30-min email to {Email}", participant.Email);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send email to {Email}", participant.Email);
                }

                try
                {
                    var notification = new CreateNotificationDto
                    {
                        SenderUserId = match.CreatorId,
                        ReceiverUserId = participant.Id,
                        Type = NotificationType.MatchReminder,
                        Title = "Match Reminder - 30 minutes",
                        Message = $"Your match '{match.Description}' starts in 30 minutes!",
                        RelatedEntityId = match.Id,
                        RelatedEntityName = "Match"
                    };

                    await notificationService.SendNotificationAsync(notification);
                    logger.LogInformation("Sent notification to {UserId}", participant.Id);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send notification to {UserId}", participant.Id);
                }
            }

            match.Reminder30MinSent = true;
            logger.LogInformation("Marked 30-min reminder as sent for match {MatchId}", match.Id);
        }
    }
}