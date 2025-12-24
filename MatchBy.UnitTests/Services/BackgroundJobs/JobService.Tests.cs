using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.BackgroundJobs;
using MatchBy.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.BackgroundJobs;

public class JobServiceTests : IDisposable
{
    private readonly Mock<ILogger<JobService>> _loggerMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly ApplicationDbContext _dbContext;
    private readonly JobService _jobService;

    public JobServiceTests()
    {
        _loggerMock = new Mock<ILogger<JobService>>();
        _emailSenderMock = new Mock<IEmailSender>();
        _memoryCacheMock = new Mock<IMemoryCache>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database with a unique name per test class
        // All contexts created with these options will share the same database
        string databaseName = Guid.NewGuid().ToString();
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        // Create a test context for setup and verification
        // This context will share the same in-memory database as contexts created by the factory
        _dbContext = new ApplicationDbContext(_dbContextOptions);

        // Setup the factory to return our in-memory context
        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(_dbContextOptions));

        _jobService = new JobService(
            _loggerMock.Object,
            dbContextFactoryMock.Object,
            _memoryCacheMock.Object,
            _emailSenderMock.Object
        );
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Helper method to create a fresh DbContext for verification.
    /// This ensures we're querying the database directly rather than using cached entities from the change tracker.
    /// </summary>
    private ApplicationDbContext CreateFreshDbContext()
    {
        return new ApplicationDbContext(_dbContextOptions);
    }

    [Fact]
    public void FireAndForgetJob_ShouldLogInformation()
    {
        // Act
        _jobService.FireAndForgetJob();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FireAndForgetJob")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithNoPendingMatches_ShouldCompleteSuccessfully()
    {
        // Arrange
        // No matches in database

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        _emailSenderMock.Verify(
            x => x.SendMatchCancelationEmail(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _emailSenderMock.Verify(
            x => x.SendMatchConfirmationEmail(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithPendingMatchWithin1Day_ShouldCancelMatchAndSendEmail()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user1",
            Email = "test@example.com",
            DisplayName = "Test User",
            UserName = "testuser"
        };

        var match = new Match
        {
            Id = "match1",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(12), // Within 1 day
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Create a fresh context to verify changes persisted in the shared database
        await using var verifyContext = new ApplicationDbContext(_dbContextOptions);
        Match? updatedMatch = await verifyContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Cancelled, updatedMatch.Status);

        _emailSenderMock.Verify(
            x => x.SendMatchCancelationEmail(creator.Email, creator.DisplayName),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithPendingMatchBetween1And3Days_ShouldSendConfirmationEmail()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user2",
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            UserName = "testuser2"
        };

        var match = new Match
        {
            Id = "match2",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2), // Between 1 and 3 days
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 2",
            Address = "Test Address 2",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Setup memory cache to return false (email not sent yet)
        object? cacheValue = null;
        _memoryCacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        ICacheEntry cacheEntry = Mock.Of<ICacheEntry>();
        _memoryCacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        Match? updatedMatch = await _dbContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Pendent, updatedMatch.Status); // Should remain pending

        _emailSenderMock.Verify(
            x => x.SendMatchConfirmationEmail(creator.Email, creator.DisplayName),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithPendingMatchBetween1And3Days_WhenEmailAlreadySent_ShouldNotSendAgain()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user3",
            Email = "test3@example.com",
            DisplayName = "Test User 3",
            UserName = "testuser3"
        };

        var match = new Match
        {
            Id = "match3",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2), // Between 1 and 3 days
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 3",
            Address = "Test Address 3",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Tennis,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 4,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Setup memory cache to return true (email already sent)
        object? cacheValue = true;
        _memoryCacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        _emailSenderMock.Verify(
            x => x.SendMatchConfirmationEmail(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithMatchInPast_ShouldMarkAsCompleted()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user4",
            Email = "test4@example.com",
            DisplayName = "Test User 4",
            UserName = "testuser4"
        };

        var match = new Match
        {
            Id = "match4",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1), // In the past
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 4",
            Address = "Test Address 4",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Volleyball,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 6,
            MaxPlayers = 12,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Use a fresh context to query the database directly and avoid change tracker cache
        await using ApplicationDbContext verifyContext = CreateFreshDbContext();
        Match? updatedMatch = await verifyContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Completed, updatedMatch.Status);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithConfirmedMatchInPast_ShouldMarkAsCompleted()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user5",
            Email = "test5@example.com",
            DisplayName = "Test User 5",
            UserName = "testuser5"
        };

        var match = new Match
        {
            Id = "match5",
            Status = MatchStatus.Confirmed,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(-2), // In the past
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 5",
            Address = "Test Address 5",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Private,
            MinPlayers = 4,
            MaxPlayers = 8,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Use a fresh context to query the database directly and avoid change tracker cache
        await using ApplicationDbContext verifyContext = CreateFreshDbContext();
        Match? updatedMatch = await verifyContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Completed, updatedMatch.Status);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithCancelledMatchInPast_ShouldNotChangeStatus()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user6",
            Email = "test6@example.com",
            DisplayName = "Test User 6",
            UserName = "testuser6"
        };

        var match = new Match
        {
            Id = "match6",
            Status = MatchStatus.Cancelled,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1), // In the past
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 6",
            Address = "Test Address 6",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        Match? updatedMatch = await _dbContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Cancelled, updatedMatch.Status); // Should remain cancelled
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithCompletedMatchInPast_ShouldNotChangeStatus()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user7",
            Email = "test7@example.com",
            DisplayName = "Test User 7",
            UserName = "testuser7"
        };

        var match = new Match
        {
            Id = "match7",
            Status = MatchStatus.Completed,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1), // In the past
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 7",
            Address = "Test Address 7",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        Match? updatedMatch = await _dbContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Completed, updatedMatch.Status); // Should remain completed
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WhenEmailSendingFails_ShouldStillUpdateMatchStatus()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user8",
            Email = "test8@example.com",
            DisplayName = "Test User 8",
            UserName = "testuser8"
        };

        var match = new Match
        {
            Id = "match8",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(12), // Within 1 day
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 8",
            Address = "Test Address 8",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Setup email sender to throw exception
        _emailSenderMock.Setup(x => x.SendMatchCancelationEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Email service unavailable"));

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Use a fresh context to query the database directly and avoid change tracker cache
        await using ApplicationDbContext verifyContext = CreateFreshDbContext();
        Match? updatedMatch = await verifyContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Cancelled, updatedMatch.Status); // Should still be cancelled

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send cancellation email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithMatchWithoutCreatorEmail_ShouldNotSendEmail()
    {
        // Arrange
        var creator = new ApplicationUser
        {
            Id = "user9",
            Email = null, // No email
            DisplayName = "Test User 9",
            UserName = "testuser9"
        };

        var match = new Match
        {
            Id = "match9",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(12), // Within 1 day
            CreatorId = creator.Id,
            Creator = creator,
            Description = "Test Match 9",
            Address = "Test Address 9",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Use a fresh context to query the database directly and avoid change tracker cache
        await using ApplicationDbContext verifyContext = CreateFreshDbContext();
        Match? updatedMatch = await verifyContext.Matches.FindAsync(match.Id);
        Assert.NotNull(updatedMatch);
        Assert.Equal(MatchStatus.Cancelled, updatedMatch.Status);

        _emailSenderMock.Verify(
            x => x.SendMatchCancelationEmail(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessMatchStatesAsync_WithMultipleMatches_ShouldProcessAllCorrectly()
    {
        // Arrange
        var creator1 = new ApplicationUser
        {
            Id = "user10",
            Email = "test10@example.com",
            DisplayName = "Test User 10",
            UserName = "testuser10"
        };

        var creator2 = new ApplicationUser
        {
            Id = "user11",
            Email = "test11@example.com",
            DisplayName = "Test User 11",
            UserName = "testuser11"
        };

        var creator3 = new ApplicationUser
        {
            Id = "user12",
            Email = "test12@example.com",
            DisplayName = "Test User 12",
            UserName = "testuser12"
        };

        var matchToCancel = new Match
        {
            Id = "match10",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddHours(12), // Within 1 day
            CreatorId = creator1.Id,
            Creator = creator1,
            Description = "Match to Cancel",
            Address = "Address 1",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        var matchToConfirm = new Match
        {
            Id = "match11",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2), // Between 1 and 3 days
            CreatorId = creator2.Id,
            Creator = creator2,
            Description = "Match to Confirm",
            Address = "Address 2",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        var matchToComplete = new Match
        {
            Id = "match12",
            Status = MatchStatus.Pendent,
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1), // In the past
            CreatorId = creator3.Id,
            Creator = creator3,
            Description = "Match to Complete",
            Address = "Address 3",
            Location = new Location(0,0,"City","Country"),
            Sport = Sports.Tennis,
            Privacy = MatchPrivacy.Public,
            MinPlayers = 2,
            MaxPlayers = 4,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        await _dbContext.Users.AddRangeAsync(creator1, creator2, creator3);
        await _dbContext.Matches.AddRangeAsync(matchToCancel, matchToConfirm, matchToComplete);
        await _dbContext.SaveChangesAsync();

        // Setup memory cache
        object? cacheValue = null;
        _memoryCacheMock.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        ICacheEntry cacheEntry = Mock.Of<ICacheEntry>();
        _memoryCacheMock.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntry);

        // Act
        await _jobService.ProcessMatchStatesAsync();

        // Assert
        // Use a fresh context to query the database directly and avoid change tracker cache
        await using ApplicationDbContext verifyContext = CreateFreshDbContext();
        Match? cancelledMatch = await verifyContext.Matches.FindAsync(matchToCancel.Id);
        Match? confirmedMatch = await verifyContext.Matches.FindAsync(matchToConfirm.Id);
        Match? completedMatch = await verifyContext.Matches.FindAsync(matchToComplete.Id);

        Assert.NotNull(cancelledMatch);
        Assert.Equal(MatchStatus.Cancelled, cancelledMatch.Status);

        Assert.NotNull(confirmedMatch);
        Assert.Equal(MatchStatus.Pendent, confirmedMatch.Status);

        Assert.NotNull(completedMatch);
        Assert.Equal(MatchStatus.Completed, completedMatch.Status);

        _emailSenderMock.Verify(
            x => x.SendMatchCancelationEmail(creator1.Email!, creator1.DisplayName),
            Times.Once);

        _emailSenderMock.Verify(
            x => x.SendMatchConfirmationEmail(creator2.Email!, creator2.DisplayName),
            Times.Once);
    }
}
