using MatchBy.Models;
using Resend;

namespace MatchBy.Services.Email;

public class EmailSender(IResend resend, ILogger<EmailSender> logger) : IEmailSender
{
    private const int MaxRetries = 3;

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
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = "MatchBy <matchby@uniqueue.site>",
                    To = email,
                    Subject = subject,
                    HtmlBody = body
                };

                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
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

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
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

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
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

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
    }

    public async Task SendMatchCancelationEmail(string email, string displayName)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Your match has been cancelled",
            HtmlBody = $"""
                        <h2>Match Cancellation Notice</h2>
                        <p>Hi {displayName},</p>
                        <p>We're sorry to inform you that your upcoming match has been cancelled.</p>
                        <p>If you have any questions, please feel free to contact our support team.</p>
                        <p>Best regards,<br/>The MatchBy Team</p>
                        """
        };
        message.To.Add(email);

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
    }

    public async Task SendMatchConfirmationEmail(string email, string displayName)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = "Confirm your upcoming match",
            HtmlBody = $"""
                        <h2>Match Confirmation Reminder</h2>
                        <p>Hi {displayName},</p>
                        <p>Great news! Your match is confirmed and ready to go.</p>
                        <p>Make sure to check the match details and prepare accordingly.</p>
                        <p>If you need to reschedule, please let us know as soon as possible.</p>
                        <p>Best regards,<br/>The MatchBy Team</p>
                        """
        };
        message.To.Add(email);

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
    }

    public async Task SendContactEmail(string name, string email, string subject, string message)
    {
        var emailMessage = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = $"Contact Form: {subject}",
            HtmlBody = $@"
                <h2>New Contact Form Submission</h2>
                <p>You have received a new message from the MatchBy contact form.</p>
                
                <h3>Contact Information</h3>
                <ul>
                    <li><strong>Name:</strong> {name}</li>
                    <li><strong>Email:</strong> {email}</li>
                    <li><strong>Subject:</strong> {subject}</li>
                </ul>
                
                <h3>Message</h3>
                <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #405d13; margin: 20px 0;'>
                    <p style='white-space: pre-wrap; margin: 0;'>{message}</p>
                </div>
                
                <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'/>
                <p style='color: #666; font-size: 12px;'>This message was sent from the MatchBy contact form.</p>
                <p style='color: #666; font-size: 12px;'>To reply, please contact: {email}</p>
            "
        };
        emailMessage.To.Add("matchby@uniqueue.site");

        var confirmationMessage = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            To = email,
            Subject = "We received your message",
            HtmlBody = $@"
                <h2>Thank you for contacting MatchBy!</h2>
                <p>Hello {name},</p>
                <p>We have received your message and will get back to you as soon as possible, usually within 24 hours.</p>
                
                <h3>Your Message Details</h3>
                <ul>
                    <li><strong>Subject:</strong> {subject}</li>
                </ul>
                
                <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #405d13; margin: 20px 0;'>
                    <p style='white-space: pre-wrap; margin: 0;'>{message}</p>
                </div>
                
                <p>If you have any urgent questions, please don't hesitate to reach out again.</p>
                <br>
                <p>Best regards,<br>The MatchBy Team</p>
            "
        };

        await resend.EmailSendAsync(emailMessage);
        await resend.EmailSendAsync(confirmationMessage);
    }

    public async Task SendMatchReminderAsync(string email, string userName, Match match, string timeframe)
    {
        var message = new EmailMessage
        {
            From = "MatchBy <matchby@uniqueue.site>",
            Subject = $"Match Reminder - {timeframe}!",
            HtmlBody = $@"
            <h2>Hello {userName}!</h2>
            <p>Your match is coming up in <strong>{timeframe}</strong>!</p>
            <h3>{match.Description}</h3>
            <p><strong>Date:</strong> {match.MatchDateTimeUtc:dddd, MMM d yyyy hh:mm tt}</p>
            <p><strong>Location:</strong> {match.Address}</p>
            <p><strong>Sport:</strong> {match.Sport}</p>
            <p>Don't forget to attend!</p>
            <br>
            <p>Best regards,<br>MatchBy Team</p>"
        };

        message.To.Add(email);
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await resend.EmailSendAsync(message);
            }
            catch (ResendException ex)
            {
                logger.LogError(ex, "Failed to send match cancellation email to {Email} on attempt {Attempt}", email,
                    attempt + 1);
                if (attempt == MaxRetries)
                {
                    throw;
                }

                await Task.Delay(1000 * (attempt + 1));
            }
        }
    }
}