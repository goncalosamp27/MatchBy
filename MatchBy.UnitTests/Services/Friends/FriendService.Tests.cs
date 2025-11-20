using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Friend;
using MatchBy.Models;
using MatchBy.Services.Friends;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Friends;

public class FriendServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateFriendDto>> _createValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly FriendService _friendService;

    public FriendServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateFriendDto>>();
        var updateValidatorMock = new Mock<IValidator<UpdateFriendDto>>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _friendService = new FriendService(
            _dbContext,
            _createValidatorMock.Object,
            updateValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetFriendshipById Tests

    [Fact]
    public async Task GetFriendshipById_WithValidId_ShouldReturnFriendship()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        
        var friendship = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<FriendDto> result = await _friendService.GetFriendshipById("friend1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("friend1", result.Data.Id);
    }

    [Fact]
    public async Task GetFriendshipById_WithInvalidId_ShouldReturnFailure()
    {
        // Act
        Result<FriendDto> result = await _friendService.GetFriendshipById("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region GetUserFriends Tests

    [Fact]
    public async Task GetUserFriends_WithValidUserId_ShouldReturnFriends()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", Email = "user3@test.com", DisplayName = "User 3" };
        
        var friendship1 = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        var friendship2 = new Friend
        {
            Id = "friend2",
            SenderId = "user3",
            ReceiverId = "user1",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.Friends.AddRangeAsync(friendship1, friendship2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<FriendDto>>> result = await _friendService.GetUserFriends("user1", 1, 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data.Count);
    }

    #endregion

    #region CreateFriendRequest Tests

    [Fact]
    public async Task CreateFriendRequest_WithValidData_ShouldCreateFriendship()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateFriendDto
        {
            SenderId = "user1",
            ReceiverId = "user2"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateFriendDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("user1", result.Data.SenderId);
        Assert.Equal("user2", result.Data.ReceiverId);
    }

    [Fact]
    public async Task CreateFriendRequest_WithExistingFriendship_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        
        var existingFriendship = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Friends.AddAsync(existingFriendship);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateFriendDto
        {
            SenderId = "user1",
            ReceiverId = "user2"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateFriendDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
    }

    #endregion

    #region RemoveFriend Tests

    [Fact]
    public async Task RemoveFriend_WithValidFriendship_ShouldRemoveFriendship()
    {
        // Arrange
        var friendship = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _friendService.RemoveFriend("friend1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        
        Friend? removedFriendship = await _dbContext.Friends.FindAsync("friend1");
        Assert.NotNull(removedFriendship!.DeletedAtUtc);
    }

    [Fact]
    public async Task RemoveFriend_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var friendship = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _friendService.RemoveFriend("friend1", "user3");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("involved in the friendship", result.ErrorMessages[0]);
    }

    #endregion

    #region CheckFriendship Tests

    [Fact]
    public async Task CheckFriendship_WithExistingFriendship_ShouldReturnTrue()
    {
        // Arrange
        var friendship = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _friendService.CheckFriendship("user1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task CheckFriendship_WithNoFriendship_ShouldReturnFalse()
    {
        // Act
        Result<bool> result = await _friendService.CheckFriendship("user1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Data);
    }

    #endregion
}


