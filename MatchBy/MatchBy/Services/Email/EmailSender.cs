using MatchBy.Models;
using Microsoft.AspNetCore.Identity;
using Resend;

namespace MatchBy.Services.Email;

public class EmailSender(IResend resend) : IEmailSender<ApplicationUser>, IMatchEmailSender
{

    public async Task SendMatchCancelledAsync(
        ApplicationUser user,
        string email,
        Match match,
        string cancelledByName)
    {

        string subject = $"Match #{match.Id} Has Been Cancelled";

        string body = $@"
        <h2>Match Cancelled</h2>
        <p>Hello {user.DisplayName},</p>
        <p>The match you were scheduled to participate in has been <strong>cancelled</strong>.</p>

        <h3>Match Details</h3>
        <ul>
            <li><strong>Sport:</strong> {match.Sport}</li>
            <li><strong>Date:</strong> {match.MatchDateTimeUtc:dddd, MMM d yyyy hh:mm tt}</li>
        </ul>

        <p>The match was cancelled by <strong>{cancelledByName}</strong>.</p>

        <br/>
        <p>Best regards,<br/>MatchBy</p>
    ";

        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            To = email,
            Subject = subject,
            HtmlBody = body
        };

        await resend.EmailSendAsync(message);
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Confirm your email",
            HtmlBody = $@"
                <h2>Hello {user.UserName}!</h2>
                <p>Thank you for signing up for MatchBy.</p>
                <p>Please confirm your email by clicking the link below:</p>
                <p><a href='{confirmationLink}' style='display: inline-block; padding: 10px 20px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px;'>Confirm Email</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{confirmationLink}</p>
                <p>If you didn't request this confirmation, you can safely ignore this email.</p>
                <br>
                <p>Best regards,<br>The MatchBy Team</p>
            "
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Reset your password",
            HtmlBody = $@"
                <h2>Hello {user.UserName}!</h2>
                <p>We received a request to reset the password for your MatchBy account.</p>
                <p>Click the link below to create a new password:</p>
                <p><a href='{resetLink}' style='display: inline-block; padding: 10px 20px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{resetLink}</p>
                <p><strong>This link will expire in a few hours.</strong></p>
                <p>If you didn't request a password reset, please ignore this email. Your password will remain unchanged.</p>
                <br>
                <p>Best regards,<br>The MatchBy Team</p>
            "
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Your password reset code",
            HtmlBody = $@"
                <h2>Hello {user.UserName}!</h2>
                <p>We received a request to reset the password for your MatchBy account.</p>
                <p>Use the code below to reset your password:</p>
                <div style='background-color: #f5f5f5; padding: 20px; text-align: center; margin: 20px 0;'>
                    <h1 style='font-family: monospace; letter-spacing: 5px; color: #333;'>{resetCode}</h1>
                </div>
                <p><strong>This code will expire in a few minutes.</strong></p>
                <p>If you didn't request this code, please ignore this email. Your password will remain unchanged.</p>
                <br>
                <p>Best regards,<br>The MatchBy Team</p>
            "
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }

    public async Task SendMatchCancelationEmail(string email, string displayName)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Your match has been cancelled",
            HtmlBody = """
                       <h2>Match Cancellation Notice</h2>
                       """
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }

    public async Task SendMatchConfirmationEmail(string email, string displayName)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Confirm your upcoming match",
            HtmlBody = """
                       <h2>Match Confirmation Reminder</h2>
                       """
        };
        message.To.Add(email);

        await resend.EmailSendAsync(message);
    }
}
