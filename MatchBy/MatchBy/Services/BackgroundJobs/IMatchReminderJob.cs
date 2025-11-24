namespace MatchBy.Services.BackgroundJobs;

public interface IMatchReminderJob
{
    Task SendRemindersAsync();
}