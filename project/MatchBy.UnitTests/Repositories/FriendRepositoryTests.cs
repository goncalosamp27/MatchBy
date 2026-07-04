using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Friend;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class FriendRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly FriendRepository _repository;

    public FriendRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new FriendRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetUserFriends Tests

    [Fact]
    public async Task GetUserFriends_WithAcceptedFriendships_ShouldReturnFriends()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true };

        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var friendship1 = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        var friendship2 = new Friend { Id = "friend2", SenderId = "user1", ReceiverId = "user3", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        var pendingFriendship = new Friend { Id = "friend3", SenderId = "user1", ReceiverId = "user4", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };

        await _dbContext.Friends.AddRangeAsync(friendship1, friendship2, pendingFriendship);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Friend>> result = await _repository.GetUserFriends("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetUserFriends_WithNoFriends_ShouldReturnEmptyList()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Friend>> result = await _repository.GetUserFriends("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetUserFriends_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.SaveChangesAsync();

        var friends = new List<Friend>();
        for (int i = 2; i <= 10; i++)
        {
            var user = new ApplicationUser { Id = $"user{i}", UserName = $"user{i}", DisplayName = $"User {i}", Email = $"user{i}@test.com", EmailConfirmed = true };
            await _dbContext.Users.AddAsync(user);
            friends.Add(new Friend { Id = $"friend{i}", SenderId = "user1", ReceiverId = $"user{i}", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i) });
        }

        await _dbContext.Friends.AddRangeAsync(friends);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Friend>> result = await _repository.GetUserFriends("user1", _dbContext, page: 2, pageSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(9, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    #endregion

    #region GetFriendRequestsSent Tests

    [Fact]
    public async Task GetFriendRequestsSent_WithPendingRequests_ShouldReturnRequests()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var pendingRequest = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };
        var acceptedRequest = new Friend { Id = "friend2", SenderId = "user1", ReceiverId = "user3", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddRangeAsync(pendingRequest, acceptedRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Friend>> result = await _repository.GetFriendRequestsSent("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(FriendStatus.Pending, result.Data[0].Status);
    }

    #endregion

    #region GetFriendRequestsReceived Tests

    [Fact]
    public async Task GetFriendRequestsReceived_WithPendingRequests_ShouldReturnRequests()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var pendingRequest = new Friend { Id = "friend1", SenderId = "user2", ReceiverId = "user1", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(pendingRequest);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Friend>> result = await _repository.GetFriendRequestsReceived("user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(FriendStatus.Pending, result.Data[0].Status);
        Assert.Equal("user2", result.Data[0].SenderId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnFriend()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Friend? result = await _repository.GetByIdAsync("friend1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("friend1", result.Id);
        Assert.Equal("user1", result.SenderId);
        Assert.Equal("user2", result.ReceiverId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Friend? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingFriendship_ShouldReturnFriend()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Friend? result = await _repository.ExistsAsync("user1", "user2", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("friend1", result.Id);
    }

    [Fact]
    public async Task ExistsAsync_WithReversedOrder_ShouldReturnFriend()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Accepted, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        Friend? result = await _repository.ExistsAsync("user2", "user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("friend1", result.Id);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentFriendship_ShouldReturnNull()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        Friend? result = await _repository.ExistsAsync("user1", "user2", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddFriendToContext()
    {
        // Arrange
        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };

        // Act
        _repository.Add(friendship, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(friendship, _dbContext.Friends);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkFriendAsModified()
    {
        // Arrange
        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        friendship.Status = FriendStatus.Accepted;

        // Act
        _repository.Update(friendship, _dbContext);

        // Assert
        EntityEntry<Friend> entry = _dbContext.Entry(friendship);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveFriendFromContext()
    {
        // Arrange
        var friendship = new Friend { Id = "friend1", SenderId = "user1", ReceiverId = "user2", Status = FriendStatus.Pending, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.Friends.AddAsync(friendship);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(friendship, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(friendship, _dbContext.Friends);
    }

    #endregion
}

