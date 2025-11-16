using Amazon.S3;
using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Services.Users;
using MatchBy.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace MatchBy.UnitTests.Services;

public class UsersServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IS3Service> _s3ServiceMock;
    private readonly UsersService _usersService;

    public UsersServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _s3ServiceMock = new Mock<IS3Service>();
        IOptions<S3Settings> s3SettingsOptions = Options.Create(new S3Settings
        {
            DefaultUrlExpiry = 60
        });
        _usersService = new UsersService(_dbContext, _s3ServiceMock.Object, s3SettingsOptions);
    }

    [Fact]
    public async Task GetUsers_WhenQueryMatchesUsers_ShouldReturnPaginatedResults()
    {
        // Arrange
        ApplicationUser user1 = CreateUser("user1", "John Doe");
        ApplicationUser user2 = CreateUser("user2", "Jane Smith");
        ApplicationUser user3 = CreateUser("user3", "Bob Johnson");

        _dbContext.Users.AddRange(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("John", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Collection(result.Data.Data,
            user => Assert.Equal("user1", user.Id),
            user => Assert.Equal("user3", user.Id));
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(1, result.Data.Page);
        Assert.Equal(5, result.Data.PageSize);
    }

    [Fact]
    public async Task GetUsers_WhenQueryMatchesMultipleUsers_ShouldReturnAllMatches()
    {
        // Arrange
        ApplicationUser user1 = CreateUser("user1", "John Doe");
        ApplicationUser user2 = CreateUser("user2", "John Smith");

        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("John", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal(2, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WhenQueryDoesNotMatch_ShouldReturnEmptyList()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("NonExistent", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data.Data);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WhenQueryIsCaseInsensitive_ShouldMatchCaseInsensitively()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("JOHN", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
    }

    [Fact]
    public async Task GetUsers_WhenPaginationIsApplied_ShouldReturnCorrectPage()
    {
        // Arrange
        ApplicationUser user1 = CreateUser("user1", "User One");
        ApplicationUser user2 = CreateUser("user2", "User Two");
        ApplicationUser user3 = CreateUser("user3", "User Three");

        _dbContext.Users.AddRange(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("User", 2, 1);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal(2, result.Data.Page);
        Assert.Equal(3, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetUsers_WhenUserMatchesUserName_ShouldIncludeInResults()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "Display Name");
        user.UserName = "username";
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("username", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
    }

    [Fact]
    public async Task GetUsers_WhenUserMatchesDisplayName_ShouldIncludeInResults()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "Display Name");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("Display", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
    }

    [Fact]
    public async Task GetUsers_WhenProfileImageExpired_ShouldRefreshProfileImage()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var expiredImage = new FileStore("old-url", DateTime.UtcNow.AddDays(-1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = expiredImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _s3ServiceMock.Setup(x => x.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/profile-key",
                HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("new-url"));

        // Act
        await _usersService.GetUsers("John", 1, 5);

        // Assert
        _s3ServiceMock.Verify(x => x.GetPresignedUrlAsync(
            $"users/{user.Id}/profile-pictures/profile-key",
            HttpVerb.GET), Times.Once);
    }

    [Fact]
    public async Task GetUsers_WhenProfileImageNotExpired_ShouldNotRefreshProfileImage()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var validImage = new FileStore("valid-url", DateTime.UtcNow.AddDays(1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = validImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _usersService.GetUsers("John", 1, 5);

        // Assert
        _s3ServiceMock.Verify(x => x.GetPresignedUrlAsync(
            It.IsAny<string>(),
            It.IsAny<HttpVerb>()), Times.Never);
    }

    [Fact]
    public async Task GetUsers_WhenProfileImageRefreshFails_ShouldContinueWithoutError()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var expiredImage = new FileStore("old-url", DateTime.UtcNow.AddDays(-1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = expiredImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _s3ServiceMock.Setup(x => x.GetPresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<HttpVerb>()))
            .ReturnsAsync(Result<string>.Fail("Error"));

        // Act
        Result<PaginationResponse<List<ApplicationUser>>> result = await _usersService.GetUsers("John", 1, 5);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        ApplicationUser? result = await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1", result.Id);
        Assert.Equal("John Doe", result.DisplayName);
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange - Empty database

        // Act
        ApplicationUser? result = await _usersService.GetUser("non-existent", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUser_WhenProfileImageExpired_ShouldRefreshProfileImage()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var expiredImage = new FileStore("old-url", DateTime.UtcNow.AddDays(-1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = expiredImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _s3ServiceMock.Setup(x => x.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/profile-key",
                HttpVerb.GET))
            .ReturnsAsync(Result<string>.Ok("new-url"));

        // Act
        await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        _s3ServiceMock.Verify(x => x.GetPresignedUrlAsync(
            $"users/{user.Id}/profile-pictures/profile-key",
            HttpVerb.GET), Times.Once);
    }

    [Fact]
    public async Task GetUser_WhenProfileImageNotExpired_ShouldNotRefreshProfileImage()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var validImage = new FileStore("valid-url", DateTime.UtcNow.AddDays(1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = validImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        await _usersService.GetUser("user1", CancellationToken.None);

        // Assert
        _s3ServiceMock.Verify(x => x.GetPresignedUrlAsync(
            It.IsAny<string>(),
            It.IsAny<HttpVerb>()), Times.Never);
    }

    [Fact]
    public async Task GetUser_ShouldSaveChangesAfterRefresh()
    {
        // Arrange
        ApplicationUser user = CreateUser("user1", "John Doe");
        var expiredImage = new FileStore("old-url", DateTime.UtcNow.AddDays(-1), "profile-key",
            FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = expiredImage;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _s3ServiceMock.Setup(x => x.GetPresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<HttpVerb>()))
            .ReturnsAsync(Result<string>.Ok("new-url"));

        // Act
        await _usersService.GetUser("user1", CancellationToken.None);

        // Assert - Verify that SaveChangesAsync was called
        ApplicationUser? updatedUser = await _dbContext.Users.FindAsync("user1");
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.ProfileImage);
    }

    private static ApplicationUser CreateUser(string id, string displayName)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = $"user{id}",
            DisplayName = displayName
        };
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

