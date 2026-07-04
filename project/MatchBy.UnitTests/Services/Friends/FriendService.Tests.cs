using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Friend;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Friend;
using MatchBy.Repositories.User;
using MatchBy.Services.Friends;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Friends;

public class FriendServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IFriendRepository> _friendRepositoryMock;
    private readonly Mock<IValidator<CreateFriendDto>> _createFriendValidatorMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly FriendService _friendService;

    public FriendServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _friendRepositoryMock = new Mock<IFriendRepository>();
        _createFriendValidatorMock = new Mock<IValidator<CreateFriendDto>>();
        _notificationServiceMock = new Mock<INotificationService>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validator to return valid by default
        _createFriendValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateFriendDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Setup notification service
        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _friendService = new FriendService(
            _userRepositoryMock.Object,
            _friendRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createFriendValidatorMock.Object,
            _notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetFriendshipById Tests

    [Fact]
    public async Task GetFriendshipById_WithValidId_ShouldReturnFriendDto()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<FriendDto> result = await _friendService.GetFriendshipById("friend1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("friend1", result.Data.Id);
        Assert.Equal(FriendStatus.Accepted, result.Data.Status);
    }

    [Fact]
    public async Task GetFriendshipById_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Friend?)null);

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
        var friends = new List<Friend>
        {
            new() { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<Friend>>
        {
            Data = friends,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _friendRepositoryMock
            .Setup(r => r.GetUserFriends("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<FriendDto>>> result = await _friendService.GetUserFriends("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal(FriendStatus.Accepted, result.Data.Data[0].Status);
    }

    #endregion

    #region GetFriendRequestsSent Tests

    [Fact]
    public async Task GetFriendRequestsSent_WithValidUserId_ShouldReturnPendingRequests()
    {
        // Arrange
        var friends = new List<Friend>
        {
            new() { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<Friend>>
        {
            Data = friends,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _friendRepositoryMock
            .Setup(r => r.GetFriendRequestsSent("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<FriendDto>>> result = await _friendService.GetFriendRequestsSent("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal(FriendStatus.Pending, result.Data.Data[0].Status);
        Assert.Equal("user1", result.Data.Data[0].SenderId);
    }

    #endregion

    #region GetFriendRequestsReceived Tests

    [Fact]
    public async Task GetFriendRequestsReceived_WithValidUserId_ShouldReturnPendingRequests()
    {
        // Arrange
        var friends = new List<Friend>
        {
            new() { Id = "friend1", SenderId = "user2", ReceiverId = "user1", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<Friend>>
        {
            Data = friends,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _friendRepositoryMock
            .Setup(r => r.GetFriendRequestsReceived("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<FriendDto>>> result = await _friendService.GetFriendRequestsReceived("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal(FriendStatus.Pending, result.Data.Data[0].Status);
        Assert.Equal("user1", result.Data.Data[0].ReceiverId);
    }

    #endregion

    #region CreateFriendRequest Tests

    [Fact]
    public async Task CreateFriendRequest_WithValidDto_ShouldCreateFriendRequest()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateFriendDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1"
        };

        var createdFriend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _friendRepositoryMock
            .Setup(r => r.ExistsAsync("sender1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Friend?)null);

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdFriend);

        // Setup Add to track the friend
        _friendRepositoryMock
            .Setup(r => r.Add(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()))
            .Callback<Friend, ApplicationDbContext>((f, db) =>
            {
                f.Id = "friend1";
            });

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _friendRepositoryMock.Verify(r => r.Add(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFriendRequest_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateFriendDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1"
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("SenderId", "Sender is required")
        });

        _createFriendValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.False(result.Success);
        _friendRepositoryMock.Verify(r => r.Add(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task CreateFriendRequest_WithNonExistentSender_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateFriendDto
        {
            SenderId = "nonexistent",
            ReceiverId = "receiver1"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender", result.ErrorMessages[0]);
        _friendRepositoryMock.Verify(r => r.Add(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task CreateFriendRequest_WithExistingFriendship_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        var existingFriend = new Friend
        {
            Id = "existing1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createDto = new CreateFriendDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _friendRepositoryMock
            .Setup(r => r.ExistsAsync("sender1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFriend);

        // Act
        Result<FriendDto> result = await _friendService.CreateFriendRequest(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
        _friendRepositoryMock.Verify(r => r.Add(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion

    #region AcceptRequest Tests

    [Fact]
    public async Task AcceptRequest_WithValidRequest_ShouldAcceptFriendship()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            Receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true }
        };

        await _dbContext.Friends.AddAsync(friend);
        await _dbContext.SaveChangesAsync();

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<FriendDto> result = await _friendService.AcceptRequest("friend1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(FriendStatus.Accepted, friend.Status);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptRequest_WithWrongReceiver_ShouldReturnFailure()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<FriendDto> result = await _friendService.AcceptRequest("friend1", "wrong-receiver");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the receiver", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptRequest_WithAlreadyAccepted_ShouldReturnFailure()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow,
            Receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true }
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<FriendDto> result = await _friendService.AcceptRequest("friend1", "receiver1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already been accepted", result.ErrorMessages[0]);
    }

    #endregion

    #region RejectRequest Tests

    [Fact]
    public async Task RejectRequest_WithValidRequest_ShouldRemoveFriendship()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.RejectRequest("friend1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _friendRepositoryMock.Verify(r => r.Remove(friend, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task RejectRequest_WithWrongReceiver_ShouldReturnFailure()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.RejectRequest("friend1", "wrong-receiver");

        // Assert
        Assert.False(result.Success);
        _friendRepositoryMock.Verify(r => r.Remove(It.IsAny<Friend>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion

    #region RemoveFriend Tests

    [Fact]
    public async Task RemoveFriend_WithValidFriendship_ShouldRemoveFriendship()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Friends.AddAsync(friend);
        await _dbContext.SaveChangesAsync();

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.RemoveFriend("friend1", "sender1");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task RemoveFriend_WithPendingStatus_ShouldReturnFailure()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.RemoveFriend("friend1", "sender1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot remove a pending", result.ErrorMessages[0]);
    }

    #endregion

    #region GetFriendshipBetweenUsers Tests

    [Fact]
    public async Task GetFriendshipBetweenUsers_WithExistingFriendship_ShouldReturnFriendDto()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "user1",
            ReceiverId = "user2",
            Status = FriendStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.ExistsAsync("user1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<FriendDto> result = await _friendService.GetFriendshipBetweenUsers("user1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("friend1", result.Data.Id);
    }

    [Fact]
    public async Task GetFriendshipBetweenUsers_WithNoFriendship_ShouldReturnNull()
    {
        // Arrange
        _friendRepositoryMock
            .Setup(r => r.ExistsAsync("user1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Friend?)null);

        // Act
        Result<FriendDto> result = await _friendService.GetFriendshipBetweenUsers("user1", "user2");

        // Assert
        Assert.True(!result.Success);
    }

    #endregion

    #region CancelFriendRequest Tests

    [Fact]
    public async Task CancelFriendRequest_WithValidRequest_ShouldRemoveRequest()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.CancelFriendRequest("friend1", "sender1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _friendRepositoryMock.Verify(r => r.Remove(friend, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CancelFriendRequest_WithNonPendingStatus_ShouldReturnFailure()
    {
        // Arrange
        var friend = new Friend
        {
            Id = "friend1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Status = FriendStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        _friendRepositoryMock
            .Setup(r => r.GetByIdAsync("friend1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(friend);

        // Act
        Result<bool> result = await _friendService.CancelFriendRequest("friend1", "sender1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only pending", result.ErrorMessages[0]);
    }

    #endregion
}
