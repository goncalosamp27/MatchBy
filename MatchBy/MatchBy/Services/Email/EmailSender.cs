using MatchBy.Models;
using Microsoft.AspNetCore.Identity;
using Resend;

namespace MatchBy.Services.Email;

public class EmailSender(IResend resend) : IEmailSender<ApplicationUser>
{
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
}
