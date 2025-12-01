using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.UnitTests.Data;

public class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;

    public ApplicationDbContextTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
    }

    [Fact]
    public void ApplicationDbContext_WhenCreated_ShouldHaveAllDbSets()
    {
        // Assert
        Assert.NotNull(_dbContext.ApplicationUsers);
        Assert.NotNull(_dbContext.Friends);
        Assert.NotNull(_dbContext.Matches);
        Assert.NotNull(_dbContext.ChatMessages);
        Assert.NotNull(_dbContext.Conversations);
        Assert.NotNull(_dbContext.MatchInvites);
        Assert.NotNull(_dbContext.PlayerRatings);
        Assert.NotNull(_dbContext.Teams);
        Assert.NotNull(_dbContext.TeamInvites);
    }

    [Fact]
    public async Task ApplicationDbContext_WhenAddingUser_ShouldSaveUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        // Act
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Assert
        ApplicationUser? savedUser = await _dbContext.Users.FindAsync("test-user-id");
        Assert.NotNull(savedUser);
        Assert.Equal("testuser", savedUser.UserName);
        Assert.Equal("test@example.com", savedUser.Email);
    }

    [Fact]
    public async Task ApplicationDbContext_WhenAddingMatch_ShouldSaveMatch()
    {
        // Arrange
        var match = new Match
        {
            Id = "test-match-id",
            Description = "Test Match",
            MinPlayers = 5,
            MaxPlayers = 10,
            CreatorId = "creator-id",
            Address = "123 Test St",
        };

        // Act
        _dbContext.Matches.Add(match);
        await _dbContext.SaveChangesAsync();

        // Assert
        Match? savedMatch = await _dbContext.Matches.FindAsync("test-match-id");
        Assert.NotNull(savedMatch);
        Assert.Equal("Test Match", savedMatch.Description);
    }

    [Fact]
    public async Task ApplicationDbContext_WhenQueryingDeletedUsers_ShouldFilterDeleted()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            Email = "user1@example.com",
            DisplayName = "User 1"
        };
        var user2 = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2",
            Email = "user2@example.com",
            DisplayName = "User 2",
            DeletedAtUtc = DateTime.UtcNow
        };

        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        List<ApplicationUser> users = await _dbContext.Users.ToListAsync();

        // Assert
        Assert.Single(users);
        Assert.Equal("user1", users[0].Id);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}

