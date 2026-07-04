using MatchBy.Data;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Repositories.User;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Users;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Users;

public class UsersServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly UsersService _usersService;

    public UsersServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database for DbContext operations (SaveChanges)
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        // Setup the factory to return our in-memory context
        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup image refresh service to return completed tasks
        _imageRefreshServiceMock
            .Setup(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()))
            .Returns(Task.CompletedTask);

        _usersService = new UsersService(
            _userRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _imageRefreshServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetUser Tests

    [Fact]
    public async Task GetUser_WithValidUserId_ShouldReturnUserDto()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            EmailConfirmed = true
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<UserDto> result = await _usersService.GetUser("user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("user1", result.Data.Id);
        Assert.Equal("Test User", result.Data.DisplayName);
        _userRepositoryMock.Verify(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(s => s.RefreshUserProfileImageAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetUser_WithInvalidUserId_ShouldReturnFailure()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<UserDto> result = await _usersService.GetUser("nonexistent");

        // Assert
        Assert.False(result.Success);
        _userRepositoryMock.Verify(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Never);
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
            Email = "test@test.com",
            EmailConfirmed = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _usersService.GetUser("user1");

        // Assert
        _imageRefreshServiceMock.Verify(
            x => x.RefreshUserProfileImageAsync(It.Is<ApplicationUser>(u => u.Id == "user1")),
            Times.Once);
    }

    [Fact]
    public async Task GetUser_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _usersService.GetUser("user1", cts.Token);
        });
        cts.Dispose();
    }

    #endregion

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_WithMatchingUsername_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "john_doe", DisplayName = "John Doe", Email = "john@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "jane_smith", DisplayName = "Jane Smith", Email = "jane@test.com", EmailConfirmed = true }
        };

        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = new List<ApplicationUser> { users[0] },
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), "john", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<UserDto>>> result = await _usersService.GetUsers("john", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("John Doe", result.Data.Data[0].DisplayName);
        Assert.Equal(1, result.Data.TotalCount);
        _imageRefreshServiceMock.Verify(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task GetUsers_WithMatchingDisplayName_ShouldReturnMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "Alice Wonder", Email = "alice@test.com", EmailConfirmed = true }
        };

        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), "Alice", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<UserDto>>> result = await _usersService.GetUsers("Alice", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("Alice Wonder", result.Data.Data[0].DisplayName);
    }

    [Fact]
    public async Task GetUsers_WithNoSearch_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
        };

        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<UserDto>>> result = await _usersService.GetUsers();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal(2, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WithNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = new List<ApplicationUser>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 10
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), "nonexistent", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<UserDto>>> result = await _usersService.GetUsers("nonexistent", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Empty(result.Data.Data);
        Assert.Equal(0, result.Data.TotalCount);
        _imageRefreshServiceMock.Verify(s => s.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetUsers_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "user4", UserName = "user4", DisplayName = "User 4", Email = "user4@test.com", EmailConfirmed = true },
            new() { Id = "user5", UserName = "user5", DisplayName = "User 5", Email = "user5@test.com", EmailConfirmed = true },
            new() { Id = "user6", UserName = "user6", DisplayName = "User 6", Email = "user6@test.com", EmailConfirmed = true }
        };

        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 10,
            Page = 2,
            PageSize = 3
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), "user", 2, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<UserDto>>> result = await _usersService.GetUsers("user", page: 2, pageSize: 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.Data.Data.Count);
        Assert.Equal(10, result.Data.TotalCount);
        Assert.Equal(2, result.Data.Page);
        Assert.Equal(3, result.Data.PageSize);
    }

    [Fact]
    public async Task GetUsers_ShouldRefreshProfileImagesForAllUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true },
            new() { Id = "2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
        };

        var paginationResponse = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _userRepositoryMock
            .Setup(r => r.GetUsers(It.IsAny<ApplicationDbContext>(), "user", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        await _usersService.GetUsers("user", page: 1, pageSize: 10);

        // Assert
        _imageRefreshServiceMock.Verify(
            x => x.RefreshUserProfileImageAsync(It.IsAny<ApplicationUser>()),
            Times.Exactly(2));
    }

    #endregion
}
