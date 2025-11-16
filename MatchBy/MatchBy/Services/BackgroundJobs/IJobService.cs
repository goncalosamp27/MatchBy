namespace MatchBy.Services.BackgroundJobs;

public interface IJobService
{
    Task ProcessMatchStatesAsync();
    void FireAndForgetJob();
}
