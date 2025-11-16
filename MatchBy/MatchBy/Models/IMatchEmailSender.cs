namespace MatchBy.Models;

public interface IMatchEmailSender
{
    Task SendMatchCancelledAsync(ApplicationUser user, string email, Match match, string cancelledByName);
}
