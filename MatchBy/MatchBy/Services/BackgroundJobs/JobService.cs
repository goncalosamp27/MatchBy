using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.BackgroundJobs;

public class JobService(ILogger<JobService> logger, IServiceProvider serviceProvider): IJobService
{
    public async Task ProcessMatchStatesAsync()
    {
        logger.LogInformation("Iniciando processamento de estados das matches...");
        
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        DateTime now = DateTime.UtcNow;
        
        List<Match> pendentMatchesBefore1Days = await dbContext.Matches
            .Where(m => m.Status == MatchStatus.Pendent && m.MatchDateTimeUtc <= now.AddDays(1))
            .ToListAsync();
        
        foreach (Match match in pendentMatchesBefore1Days)
        {
            logger.LogInformation("Send email to creator to inform match is cancelled");
            match.Status = MatchStatus.Cancelled;
        }
        
        List<Match> pendentMatchesBefore3Days = await dbContext.Matches
            .Where(m => m.Status == MatchStatus.Pendent && m.MatchDateTimeUtc <= now.AddDays(3))
            .Except(pendentMatchesBefore1Days)
            .ToListAsync();
            
        foreach (Match match in pendentMatchesBefore3Days)
        {
            logger.LogInformation("Send email to creator to confirm the match");
        }
        
        List<Match> matchesToFinish = await dbContext.Matches
            .Where(m => m.Status != MatchStatus.Completed && m.MatchDateTimeUtc >= now)
            .ToListAsync();
        
        foreach (Match match in matchesToFinish)
        {
            match.Status = MatchStatus.Completed;
            logger.LogInformation("Match {MatchId} finalizada", match.Id);
        }
        
        await dbContext.SaveChangesAsync();
        
        logger.LogInformation(
           "Processamento de estados das matches concluído. {PendentMatchesBefore1Days} matches canceladas, {PendentMatchesBefore3Days} avisos enviados, {MatchesToFinish} matches finalizadas.",
            pendentMatchesBefore1Days.Count,
            pendentMatchesBefore3Days.Count,
            matchesToFinish.Count
        );
    }

    public void FireAndForgetJob()
    {
        logger.LogInformation("FireAndForgetJob");
    }
}
