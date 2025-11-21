using MatchBy.Models;
using Microsoft.AspNetCore.Identity;

namespace MatchBy.Services.Email;

public interface IEmailSender: IEmailSender<ApplicationUser>
{
    Task SendMatchCancelationEmail(string email, string displayName);
    Task SendMatchConfirmationEmail(string email, string displayName);
    Task SendMatchCancelledAsync(ApplicationUser user, string email, Match match, string cancelledByName);
    Task SendContactEmail(string name, string email, string subject, string message);
}
