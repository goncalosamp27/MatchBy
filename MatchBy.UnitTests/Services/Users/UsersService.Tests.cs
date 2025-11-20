using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MatchBy.UnitTests.Services.Users;

public class UsersServiceTests : IDisposable
{
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly UsersService _usersService;

    public UsersServiceTests()
    {
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();

        // Setup in-memory database
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _usersService = new UsersService(_dbContext, _imageRefreshServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_WithMatchingUsername_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "john_doe", DisplayName = "John Doe", Email = "john@test.com" },
            new() { Id = "2", UserName = "jane_smith", DisplayName = "Jane Smith", Email = "jane@test.com" },
            new() { Id = "3", UserName = "bob_jones", DisplayName = "Bob Jones", Email = "bob@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("john", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("john_doe", result.Data.Data[0].UserName);
        Assert.Equal(1, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WithMatchingDisplayName_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "Alice Wonder", Email = "alice@test.com" },
            new() { Id = "2", UserName = "user2", DisplayName = "Bob Builder", Email = "bob@test.com" },
            new() { Id = "3", UserName = "user3", DisplayName = "Charlie Brown", Email = "charlie@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("Alice", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Data);
        Assert.Equal("Alice Wonder", result.Data.Data[0].DisplayName);
    }

    [Fact]
    public async Task GetUsers_WithCaseInsensitiveSearch_ShouldReturnMatches()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "TestUser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("testuser", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Data);
    }

    [Fact]
    public async Task GetUsers_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "john_doe",
            DisplayName = "John Doe",
            Email = "john@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("nonexistent", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data!.Data);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        for (int i = 1; i <= 10; i++)
        {
            users.Add(new ApplicationUser
            {
                Id = $"user{i}",
                UserName = $"user{i}",
                DisplayName = $"User {i}",
                Email = $"user{i}@test.com"
            });
        }

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("user", page: 2, pageSize: 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Data.Count);
        Assert.Equal(10, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Page);
        Assert.Equal(3, result.Data.PageSize);
    }

    [Fact]
    public async Task GetUsers_ShouldOrderByUsername()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "charlie", DisplayName = "Charlie", Email = "c@test.com" },
            new() { Id = "2", UserName = "alice", DisplayName = "Alice", Email = "a@test.com" },
            new() { Id = "3", UserName = "bob", DisplayName = "Bob", Email = "b@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.Data.Count);
        Assert.Equal("alice", result.Data.Data[0].UserName);
        Assert.Equal("bob", result.Data.Data[1].UserName);
        Assert.Equal("charlie", result.Data.Data[2].UserName);
    }

    [Fact]
    public async Task GetUsers_ShouldRefreshProfileImagesForAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com" },
            new() { Id = "2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        await _usersService.GetUsers("user", page: 1, pageSize: 10);

        // Assert
        _imageRefreshServiceMock.Verify(
            x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetUsers_WithPartialMatch_ShouldReturnAllMatches()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "developer", DisplayName = "Dev User", Email = "dev@test.com" },
            new() { Id = "2", UserName = "designer", DisplayName = "Design User", Email = "design@test.com" },
            new() { Id = "3", UserName = "manager", DisplayName = "Mgr User", Email = "mgr@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("de", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Data.Count); // developer and designer
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_WithValidUserId_ShouldReturnUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1", result.Id);
        Assert.Equal("testuser", result.UserName);
    }

    [Fact]
    public async Task GetUser_WithInvalidUserId_ShouldReturnNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _usersService.GetUser("nonexistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUser_ShouldRefreshProfileImage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        _imageRefreshServiceMock.Verify(
            x => x.RefreshUserProfileImageAsync(It.Is<ApplicationUser>(u => u.Id == "user1")),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_ShouldSaveChangesAfterRefresh()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            ProfileImage = new FileStore(
                Url: "old-url",
                ExpireDateTimeUtc: DateTime.UtcNow.AddHours(-1),
                Key: "key",
                FileCategory: FileCategory.ProfileImage,
                FileType: FileType.Image,
                CreatedAtUtc: DateTime.UtcNow
            )
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Setup mock to modify the user's profile image
        _imageRefreshServiceMock
            .Setup(x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u =>
            {
                if (u.ProfileImage != null)
                {
                    u.ProfileImage = u.ProfileImage with { Url = "new-url" };
                }
            });

        // Act
        ApplicationUser? result = await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new-url", result.ProfileImage?.Url);
    }

    [Fact]
    public async Task GetUser_WhenUserNotFound_ShouldNotRefreshImage()
    {
        // Act
        ApplicationUser? result = await _usersService.GetUser("nonexistent", CancellationToken.None);

        // Assert
        Assert.Null(result);
        _imageRefreshServiceMock.Verify(
            x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()),
            Times.Never);
    }

    [Fact]
    public async Task GetUser_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _usersService.GetUser("user1", cts.Token);
        });
        cts.Dispose();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetUsers_WithEmptyQuery_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com" },
            new() { Id = "2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com" }
        };

        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Data.Count);
    }

    [Fact]
    public async Task GetUsers_WithPageBeyondResults_ShouldReturnEmptyList()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "1",
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com"
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("user", page: 10, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data!.Data);
        Assert.Equal(1, result.Data.TotalCount); // Total count should still be correct
    }

    #endregion
}
