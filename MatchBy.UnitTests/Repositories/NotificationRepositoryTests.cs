using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class NotificationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationRepository _repository;

    public NotificationRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new NotificationRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetNotificationsAsync Tests

    [Fact]
    public async Task GetNotificationsAsync_WithValidUserId_ShouldReturnNotifications()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notifications = new List<Notification>
        {
            new() { Id = "notif1", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 1", Message = "Message 1", RelatedEntityId = "entity1", RelatedEntityName = "Entity1", IsRead = false, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "notif2", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 2", Message = "Message 2", RelatedEntityId = "entity2", RelatedEntityName = "Entity2", IsRead = false, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Notifications.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Notification>> result = await _repository.GetNotificationsAsync("receiver1", pageSize: 10, cursor: null, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithCursor_ShouldReturnNotificationsBeforeCursor()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notifications = new List<Notification>
        {
            new() { Id = "notif1", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 1", Message = "Message 1", RelatedEntityId = "entity1", RelatedEntityName = "Entity1", IsRead = false, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "notif2", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 2", Message = "Message 2", RelatedEntityId = "entity2", RelatedEntityName = "Entity2", IsRead = false, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "notif3", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 3", Message = "Message 3", RelatedEntityId = "entity3", RelatedEntityName = "Entity3", IsRead = false, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Notifications.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Notification>> result = await _repository.GetNotificationsAsync("receiver1", pageSize: 2, cursor: "notif3", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(n => string.Compare(n.Id, "notif3", StringComparison.Ordinal) < 0));
    }

    [Fact]
    public async Task GetNotificationsAsync_WithPageSize_ShouldLimitResults()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notifications = new List<Notification>();
        for (int i = 1; i <= 5; i++)
        {
            notifications.Add(new Notification
            {
                Id = $"notif{i}",
                ReceiverId = "receiver1",
                SenderId = "sender1",
                Type = NotificationType.Friend,
                Title = $"Title {i}",
                Message = $"Message {i}",
                RelatedEntityId = $"entity{i}",
                RelatedEntityName = $"Entity{i}",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.Notifications.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Notification>> result = await _repository.GetNotificationsAsync("receiver1", pageSize: 3, cursor: null, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.NotNull(result.NextCursor);
    }

    #endregion

    #region GetUnreadNotificationsAsync Tests

    [Fact]
    public async Task GetUnreadNotificationsAsync_ShouldReturnOnlyUnreadNotifications()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notifications = new List<Notification>
        {
            new() { Id = "notif1", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 1", Message = "Message 1", RelatedEntityId = "entity1", RelatedEntityName = "Entity1", IsRead = false, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "notif2", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 2", Message = "Message 2", RelatedEntityId = "entity2", RelatedEntityName = "Entity2", IsRead = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "notif3", ReceiverId = "receiver1", SenderId = "sender1", Type = NotificationType.Friend, Title = "Title 3", Message = "Message 3", RelatedEntityId = "entity3", RelatedEntityName = "Entity3", IsRead = false, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Notifications.AddRangeAsync(notifications);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Notification>> result = await _repository.GetUnreadNotificationsAsync("receiver1", pageSize: 10, cursor: null, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(n => !n.IsRead));
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnNotification()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notification = new Notification
        {
            Id = "notif1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Type = NotificationType.Friend,
            Title = "Title",
            Message = "Message",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity1",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        Notification? result = await _repository.GetByIdAsync("notif1", "receiver1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("notif1", result.Id);
        Assert.Equal("Title", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentReceiver_ShouldReturnNull()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var notification = new Notification
        {
            Id = "notif1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Type = NotificationType.Friend,
            Title = "Title",
            Message = "Message",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity1",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        Notification? result = await _repository.GetByIdAsync("notif1", "different-receiver", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddNotificationToContext()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "notif1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Type = NotificationType.Friend,
            Title = "Title",
            Message = "Message",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity1",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(notification, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(notification, _dbContext.Notifications);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkNotificationAsModified()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "notif1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Type = NotificationType.Friend,
            Title = "Title",
            Message = "Message",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity1",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        notification.IsRead = true;

        // Act
        _repository.Update(notification, _dbContext);

        // Assert
        EntityEntry<Notification> entry = _dbContext.Entry(notification);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveNotificationFromContext()
    {
        // Arrange
        var notification = new Notification
        {
            Id = "notif1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Type = NotificationType.Friend,
            Title = "Title",
            Message = "Message",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity1",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(notification, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(notification, _dbContext.Notifications);
    }

    #endregion
}

