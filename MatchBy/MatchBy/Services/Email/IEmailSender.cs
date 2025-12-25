using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity;

namespace MatchBy.Services.Email;

public interface IEmailSender : IEmailSender<ApplicationUser>
{
    Task SendMatchCancelationEmail(string email, string displayName);
    Task SendMatchConfirmationEmail(string email, string displayName);
    Task SendMatchCancelledAsync(string userDisplayName, string email, string matchId, Sports matchSport, DateTime matchDateTimeUtc, string cancelledByName);
    Task SendContactEmail(string name, string email, string subject, string message);
    Task SendMatchReminderAsync(string email, string userName, string matchDescription, DateTime matchDateTime, string matchAddress, Sports matchSport, string timeframe);
}