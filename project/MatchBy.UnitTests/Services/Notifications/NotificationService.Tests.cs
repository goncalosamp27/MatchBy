using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Hubs;
using MatchBy.Models;
using MatchBy.Repositories.Notification;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Notifications;

public class NotificationServiceTests : IDisposable
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<IValidator<CreateNotificationDto>> _createNotificationValidatorMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _createNotificationValidatorMock = new Mock<IValidator<CreateNotificationDto>>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validator to return valid by default
        _createNotificationValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _notificationService = new NotificationService(
            _notificationRepositoryMock.Object,
            _hubContextMock.Object,
            _createNotificationValidatorMock.Object,
            _imageRefreshServiceMock.Object,
            _dbContextFactoryMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetNotifications Tests

    [Fact]
    public async Task GetNotifications_WithValidParameters_ShouldReturnNotifications()
    {
        // Arrange
        int pageSize = 10;
        string userId = "user1";
        string? cursor = (string?)null;

        var notifications = new List<Notification>
        {
            new()
            {
                Id = "notif1",
                ReceiverId = userId,
                SenderId = "sender1",
                Type = NotificationType.Match,
                Title = "New match",
                Message = "You have a new match invite",
                RelatedEntityId = "match1",
                RelatedEntityName = "Football Match",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var paginationResponse = new CursorPaginationResponse<List<Notification>>
        {
            Data = notifications,
            NextCursor = "next-cursor"
        };

        _notificationRepositoryMock
            .Setup(r => r.GetNotificationsAsync(userId, pageSize, cursor, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshNotificationImagesAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<CursorPaginationResponse<List<NotificationDto>>> result = await _notificationService.GetNotifications(pageSize, userId, cursor);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("notif1", result.Data.Data[0].Id);
        Assert.Equal("next-cursor", result.Data.NextCursor);

        _imageRefreshServiceMock.Verify(s => s.RefreshNotificationImagesAsync(It.IsAny<Notification>()), Times.Once);
    }

    #endregion

    #region ReadNotification Tests

    [Fact]
    public async Task ReadNotification_WithValidNotification_ShouldMarkAsRead()
    {
        // Arrange
        string notificationId = "notif1";
        string userId = "user1";

        var notification = new Notification
        {
            Id = notificationId,
            ReceiverId = userId,
            SenderId = "sender1",
            Type = NotificationType.Match,
            Title = "New match",
            Message = "You have a new match invite",
            RelatedEntityId = "match1",
            RelatedEntityName = "Football Match",
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        _notificationRepositoryMock
            .Setup(r => r.GetByIdAsync(notificationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshNotificationImagesAsync(notification))
            .Returns(Task.CompletedTask);

        // Act
        Result<NotificationDto> result = await _notificationService.ReadNotification(notificationId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(notificationId, result.Data.Id);
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAtUtc);

        _imageRefreshServiceMock.Verify(s => s.RefreshNotificationImagesAsync(notification), Times.Once);
    }

    [Fact]
    public async Task ReadNotification_WithNonExistentNotification_ShouldReturnFailure()
    {
        // Arrange
        string notificationId = "nonexistent";
        string userId = "user1";

        _notificationRepositoryMock
            .Setup(r => r.GetByIdAsync(notificationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        Result<NotificationDto> result = await _notificationService.ReadNotification(notificationId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Notification not found", result.ErrorMessages[0]);
    }

    #endregion

    #region MarkAllNotificationsAsReadAsync Tests

    [Fact]
    public async Task MarkAllNotificationsAsReadAsync_WithUnreadNotifications_ShouldMarkAllAsRead()
    {
        // Arrange
        string userId = "user1";

        var unreadNotifications = new List<Notification>
        {
            new()
            {
                Id = "notif1",
                ReceiverId = userId,
                SenderId = "sender1",
                Type = NotificationType.Match,
                Title = "Notification 1",
                Message = "Message 1",
                RelatedEntityId = "entity1",
                RelatedEntityName = "Entity 1",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = "notif2",
                ReceiverId = userId,
                SenderId = "sender2",
                Type = NotificationType.Match,
                Title = "Notification 2",
                Message = "Message 2",
                RelatedEntityId = "entity2",
                RelatedEntityName = "Entity 2",
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var paginationResponse = new CursorPaginationResponse<List<Notification>>
        {
            Data = unreadNotifications,
            NextCursor = null
        };

        _notificationRepositoryMock
            .Setup(r => r.GetUnreadNotificationsAsync(userId, int.MaxValue, null, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshNotificationImagesAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<int> result = await _notificationService.MarkAllNotificationsAsReadAsync(userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data);

        _imageRefreshServiceMock.Verify(s => s.RefreshNotificationImagesAsync(It.IsAny<Notification>()), Times.Exactly(2));
    }


    [Fact]
    public async Task SendNotificationAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var notificationDto = new CreateNotificationDto
        {
            Type = NotificationType.Match,
            ReceiverUserId = "",
            SenderUserId = "sender1",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity 1",
            Title = "Invalid notification",
            Message = "Test message"
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("ReceiverUserId", "Receiver ID is required")
        });

        _createNotificationValidatorMock
            .Setup(v => v.ValidateAsync(notificationDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<bool> result = await _notificationService.SendNotificationAsync(notificationDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Receiver ID is required", result.ErrorMessages[0]);

        _notificationRepositoryMock.Verify(r => r.Add(It.IsAny<Notification>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task SendNotificationAsync_WhenFailedToCreateNotification_ShouldReturnFailure()
    {
        // Arrange
        var notificationDto = new CreateNotificationDto
        {
            Type = NotificationType.Match,
            ReceiverUserId = "receiver1",
            SenderUserId = "sender1",
            RelatedEntityId = "entity1",
            RelatedEntityName = "Entity 1",
            Title = "Test notification",
            Message = "Test message"
        };

        _notificationRepositoryMock
            .Setup(r => r.Add(It.IsAny<Notification>(), It.IsAny<ApplicationDbContext>()))
            .Callback<Notification, ApplicationDbContext>((n, db) =>
            {
                n.Id = "notif1";
            });

        _notificationRepositoryMock
            .Setup(r => r.GetByIdAsync("notif1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        Result<bool> result = await _notificationService.SendNotificationAsync(notificationDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Failed to create notification", result.ErrorMessages[0]);
    }

    #endregion
}
