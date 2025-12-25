using MatchBy.Data;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.BackgroundJobs;
using MatchBy.Services.Email;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.BackgroundJobs;

public class MatchReminderJobTests : IDisposable
{
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ILogger<MatchReminderJob>> _loggerMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchReminderJob _matchReminderJob;

    public MatchReminderJobTests()
    {
        _emailSenderMock = new Mock<IEmailSender>();
        var contextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _loggerMock = new Mock<ILogger<MatchReminderJob>>();
        _notificationServiceMock = new Mock<INotificationService>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        contextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        _matchReminderJob = new MatchReminderJob(
            _emailSenderMock.Object,
            contextFactoryMock.Object,
            _loggerMock.Object,
            _notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region SendRemindersAsync Tests

    [Fact]
    public async Task SendRemindersAsync_ShouldSend3DayAnd30MinReminders()
    {
        // Arrange
        var matchFor3DayReminder = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "3 Day Reminder Match",
            Sport = Sports.Football,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(48), // Within 3 days from now
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User 1",
                    Email = "user1@example.com",
                    EmailConfirmed = true
                }
            }
        };

        var matchFor30MinReminder = new Match
        {
            Id = "match2",
            Address = "456 Oak St",
            CreatorId = "creator2",
            Description = "30 Min Reminder Match",
            Sport = Sports.Basketball,
            MatchDateTimeUtc = DateTime.UtcNow.AddMinutes(25), // Within 30 minutes from now
            Status = MatchStatus.Confirmed,
            Reminder30MinSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user2",
                    UserName = "user2",
                    DisplayName = "User 2",
                    Email = "user2@example.com",
                    EmailConfirmed = true
                }
            }
        };

        var matches = new List<Match> { matchFor3DayReminder, matchFor30MinReminder };
        _dbContext.Matches.AddRange(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Reload matches from database to check changes
        Match? updatedMatch1 = await _dbContext.Matches.FindAsync("match1");
        Match? updatedMatch2 = await _dbContext.Matches.FindAsync("match2");

        // Assert
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(
            "user1@example.com", "user1", "3 Day Reminder Match", It.IsAny<DateTime>(), "123 Main St", Sports.Football, "3 days"), Times.Once);
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(
            "user2@example.com", "user2", "30 Min Reminder Match", It.IsAny<DateTime>(), "456 Oak St", Sports.Basketball, "30 minutes"), Times.Once);

        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.Is<CreateNotificationDto>(dto =>
            dto.ReceiverUserId == "user1" &&
            dto.Type == NotificationType.Match &&
            dto.Title.Contains("3 days") &&
            dto.RelatedEntityId == "match1"), CancellationToken.None), Times.Once);

        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.Is<CreateNotificationDto>(dto =>
            dto.ReceiverUserId == "user2" &&
            dto.Type == NotificationType.Match &&
            dto.Title.Contains("30 minutes") &&
            dto.RelatedEntityId == "match2"), CancellationToken.None), Times.Once);

        Assert.False(updatedMatch1!.Reminder3DaysSent);
        Assert.False(updatedMatch2!.Reminder30MinSent);
    }

    [Fact]
    public async Task SendRemindersAsync_WithEmailSendingFailure_ShouldContinueAndLogError()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Test Match",
            Sport = Sports.Football,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(48), // Within 3 days from now
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User 1",
                    Email = "user1@example.com",
                    EmailConfirmed = true
                }
            }
        };

        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();

        _emailSenderMock
            .Setup(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email sending failed"));

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Reload match from database
        Match? updatedMatch = await _dbContext.Matches.FindAsync("match1");

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Should still mark as sent and send notification
        Assert.False(updatedMatch!.Reminder3DaysSent);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SendRemindersAsync_WithNotificationFailure_ShouldContinueAndLogError()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Test Match",
            Sport = Sports.Football,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(48), // Within 3 days from now
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User 1",
                    Email = "user1@example.com",
                    EmailConfirmed = true
                }
            }
        };

        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();

        _notificationServiceMock
            .Setup(n => n.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), CancellationToken.None))
            .ThrowsAsync(new Exception("Notification sending failed"));

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Reload match from database
        Match? updatedMatch = await _dbContext.Matches.FindAsync("match1");

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Should still mark as sent and send email
        Assert.False(updatedMatch!.Reminder3DaysSent);
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region Send3DayRemindersAsync Tests

    [Fact]
    public async Task Send3DayRemindersAsync_WithNoEligibleMatches_ShouldNotSendAnyReminders()
    {
        // Arrange
        var now = new DateTime(2025, 12, 25, 12, 0, 0, DateTimeKind.Utc);

        // Match that's already had reminder sent
        var matchAlreadySent = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Already Sent",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddDays(2),
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = true, // Already sent
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@example.com", EmailConfirmed = true }
            }
        };

        // Match that's too far in the future
        var matchTooFar = new Match
        {
            Id = "match2",
            Address = "456 Oak St",
            CreatorId = "creator2",
            Description = "Too Far",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddDays(5), // Beyond 3 days
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@example.com", EmailConfirmed = true }
            }
        };

        // Match that's in the past
        var matchInPast = new Match
        {
            Id = "match3",
            Address = "789 Pine St",
            CreatorId = "creator3",
            Description = "In Past",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddDays(-1),
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@example.com", EmailConfirmed = true }
            }
        };

        _dbContext.Matches.AddRange(matchAlreadySent, matchTooFar, matchInPast);
        await _dbContext.SaveChangesAsync();

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Assert
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Send3DayRemindersAsync_WithUserWithoutEmail_ShouldSkipEmailButSendNotification()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Test Match",
            Sport = Sports.Football,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(48), // Within 3 days from now
            Status = MatchStatus.Pendent,
            Reminder3DaysSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User 1",
                    Email = null, // No email
                    EmailConfirmed = true
                }
            }
        };

        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Reload match from database
        Match? updatedMatch = await _dbContext.Matches.FindAsync("match1");

        // Assert
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), CancellationToken.None), Times.Once);
        Assert.False(updatedMatch!.Reminder3DaysSent);
    }

    #endregion

    #region Send30MinRemindersAsync Tests

    [Fact]
    public async Task Send30MinRemindersAsync_WithNoEligibleMatches_ShouldNotSendAnyReminders()
    {
        // Arrange
        var now = new DateTime(2025, 12, 25, 12, 0, 0, DateTimeKind.Utc);

        // Match that's already had reminder sent
        var matchAlreadySent = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Already Sent",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddMinutes(25),
            Status = MatchStatus.Confirmed,
            Reminder30MinSent = true, // Already sent
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@example.com", EmailConfirmed = true }
            }
        };

        // Match that's too far in the future
        var matchTooFar = new Match
        {
            Id = "match2",
            Address = "456 Oak St",
            CreatorId = "creator2",
            Description = "Too Far",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddHours(2), // Beyond 30 minutes
            Status = MatchStatus.Confirmed,
            Reminder30MinSent = false,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@example.com", EmailConfirmed = true }
            }
        };

        // Match that's not confirmed
        var matchNotConfirmed = new Match
        {
            Id = "match3",
            Address = "789 Pine St",
            CreatorId = "creator3",
            Description = "Not Confirmed",
            Sport = Sports.Football,
            MatchDateTimeUtc = now.AddMinutes(25),
            Status = MatchStatus.Pendent, // Not confirmed
            Reminder30MinSent = false,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@example.com", EmailConfirmed = true }
            }
        };

        _dbContext.Matches.AddRange(matchAlreadySent, matchTooFar, matchNotConfirmed);
        await _dbContext.SaveChangesAsync();

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Assert
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task Send30MinRemindersAsync_WithUserWithoutEmail_ShouldSkipEmailButSendNotification()
    {
        // Arrange
        var match = new Match
        {
            Id = "match1",
            Address = "123 Main St",
            CreatorId = "creator1",
            Description = "Test Match",
            Sport = Sports.Football,
            MatchDateTimeUtc = DateTime.UtcNow.AddMinutes(25), // Within 30 minutes from now
            Status = MatchStatus.Confirmed,
            Reminder30MinSent = false,
            Participants = new List<ApplicationUser>
            {
                new()
                {
                    Id = "user1",
                    UserName = "user1",
                    DisplayName = "User 1",
                    Email = null, // No email
                    EmailConfirmed = true
                }
            }
        };

        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _matchReminderJob.SendRemindersAsync();

        // Reload match from database
        Match? updatedMatch = await _dbContext.Matches.FindAsync("match1");

        // Assert
        _emailSenderMock.Verify(e => e.SendMatchReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<Sports>(), It.IsAny<string>()), Times.Never);
        Assert.False(updatedMatch!.Reminder30MinSent);
    }

    #endregion
}
