using MatchBy.Models;
using MatchBy.Services.Email;
using MatchBy.Enums;
using Moq;
using Resend;

namespace MatchBy.UnitTests.Services.Email;

public class EmailSenderTests
{
    private readonly Mock<IResend> _resendMock;
    private readonly EmailSender _emailSender;

    public EmailSenderTests()
    {
        _resendMock = new Mock<IResend>();
        var loggerMock = new Mock<ILogger<EmailSender>>();
        _emailSender = new EmailSender(_resendMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task SendConfirmationLinkAsync_CallsEmailService()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "johndoe" };
        string email = "test@example.com";
        string confirmationLink = "https://example.com/confirm/123";

        // Act
        await _emailSender.SendConfirmationLinkAsync(user, email, confirmationLink);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_CallsEmailService()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "johndoe" };
        string email = "test@example.com";
        string resetLink = "https://example.com/reset/123";

        // Act
        await _emailSender.SendPasswordResetLinkAsync(user, email, resetLink);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_CallsEmailService()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "johndoe" };
        string email = "test@example.com";
        string resetCode = "123456";

        // Act
        await _emailSender.SendPasswordResetCodeAsync(user, email, resetCode);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendMatchCancelationEmail_CallsEmailService()
    {
        // Arrange
        string email = "test@example.com";
        string displayName = "John Doe";

        // Act
        await _emailSender.SendMatchCancelationEmail(email, displayName);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendMatchConfirmationEmail_CallsEmailService()
    {
        // Arrange
        string email = "test@example.com";
        string displayName = "John Doe";

        // Act
        await _emailSender.SendMatchConfirmationEmail(email, displayName);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendContactEmail_SendsTwoEmails()
    {
        // Arrange
        string name = "Test User";
        string email = "test@example.com";
        string subject = "Test Subject";
        string message = "Test message";

        // Act
        await _emailSender.SendContactEmail(name, email, subject, message);

        // Assert - SendContactEmail sends both a contact email and confirmation email
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task SendMatchReminderAsync_CallsEmailService()
    {
        // Arrange
        string email = "test@example.com";
        string userName = "John Doe";
        string matchDescription = "Weekly Football Match";
        var matchDateTime = new DateTime(2024, 12, 25, 15, 0, 0, DateTimeKind.Utc);
        string matchAddress = "Central Park";
        Sports matchSport = Sports.Football;
        string timeframe = "3 days";

        // Act
        await _emailSender.SendMatchReminderAsync(email, userName, matchDescription, matchDateTime, matchAddress, matchSport, timeframe);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendMatchCancelledAsync_CallsEmailService()
    {
        // Arrange
        string userDisplayName = "John Doe";
        string email = "test@example.com";
        string matchId = "match123";
        Sports matchSport = Sports.Football;
        var matchDateTime = new DateTime(2024, 12, 25, 15, 0, 0, DateTimeKind.Utc);
        string cancelledByName = "Jane Smith";

        // Act
        await _emailSender.SendMatchCancelledAsync(userDisplayName, email, matchId, matchSport, matchDateTime, cancelledByName);

        // Assert
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task EmailSender_HandlesAllEmailTypes()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "testuser", DisplayName = "Test User" };
        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            MatchDateTimeUtc = new DateTime(2024, 12, 25, 15, 0, 0, DateTimeKind.Utc),
            Description = "Test Match",
            Address = "Test Location"
        };

        // Act - Test all email types
        await _emailSender.SendConfirmationLinkAsync(user, "test@example.com", "https://example.com/confirm/123");
        await _emailSender.SendPasswordResetLinkAsync(user, "test@example.com", "https://example.com/reset/123");
        await _emailSender.SendPasswordResetCodeAsync(user, "test@example.com", "123456");
        await _emailSender.SendMatchCancelledAsync(user.DisplayName!, "test@example.com", match.Id, match.Sport, match.MatchDateTimeUtc, "Admin");
        await _emailSender.SendMatchCancelationEmail("test@example.com", "Test User");
        await _emailSender.SendMatchConfirmationEmail("test@example.com", "Test User");
        await _emailSender.SendMatchReminderAsync("test@example.com", "Test User", match.Description, match.MatchDateTimeUtc, match.Address, match.Sport, "24 hours");
        await _emailSender.SendContactEmail("Test User", "test@example.com", "Test", "Message");

        // Assert - Should have sent 8 emails (confirmation link) + 8 emails (other types) + 2 emails (contact) = 9 total
        _resendMock.Verify(x => x.EmailSendAsync(It.IsAny<EmailMessage>(), CancellationToken.None), Times.Exactly(9));
    }
}
