using FluentValidation;
using FluentValidation.Results;
using MatchBy.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;
using MatchBy.Hubs;
using Microsoft.EntityFrameworkCore;
using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Repositories.Notification;
using MatchBy.Services.ImageRefresh;

namespace MatchBy.Services.Notifications;

public class NotificationService(
    INotificationRepository notificationRepository,
    IHubContext<NotificationHub> hubContext,
    IValidator<CreateNotificationDto> createNotificationValidator,
    IImageRefreshService imageRefreshService,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : INotificationService
{
    /// <summary>
    /// Retrieves a paginated list of notifications for a specific user using cursor-based pagination.
    /// </summary>
    /// <param name="pageSize">The number of notifications to retrieve per page.</param>
    /// <param name="userId">The unique identifier of the user to get notifications for.</param>
    /// <param name="cursor">Optional cursor for pagination. If provided, returns notifications before this cursor.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a cursor-paginated response with a list of notification DTOs, ordered by ID (newest first).
    /// </returns>
    /// <remarks>
    /// Notifications are refreshed in parallel for better performance. The cursor is based on the notification ID.
    /// </remarks>
    public async Task<Result<CursorPaginationResponse<List<NotificationDto>>>> GetNotifications(int pageSize, string userId, string? cursor, CancellationToken ct = default)
    {        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        CursorPaginationResponse<List<Notification>> notifications = await notificationRepository.GetNotificationsAsync(userId, pageSize, cursor, dbContext, ct);

        IEnumerable<Task> tasks = notifications.Data.Select(imageRefreshService.RefreshNotificationImagesAsync);
        await Task.WhenAll(tasks);
        
        var notificationsDtos = notifications.Data.Select(m => m.ToDto()).ToList();
    
        return Result<CursorPaginationResponse<List<NotificationDto>>>.Ok(
            new CursorPaginationResponse<List<NotificationDto>>
            {
                Data = notificationsDtos,
                NextCursor = notifications.NextCursor
            });
    }
    /// <summary>
    /// Marks a notification as read for a specific user.
    /// </summary>
    /// <param name="notificationId">The unique identifier of the notification to mark as read.</param>
    /// <param name="userId">The unique identifier of the user marking the notification as read.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the updated notification DTO if successful, or an error message if the notification is not found.
    /// </returns>
    public async Task<Result<NotificationDto>> ReadNotification(string notificationId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Notification? notification = await notificationRepository.GetByIdAsync(notificationId, userId, dbContext, ct);
        if (notification == null)
        {
            return Result<NotificationDto>.Fail("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;

        await imageRefreshService.RefreshNotificationImagesAsync(notification);

        await dbContext.SaveChangesAsync(ct);

        return Result<NotificationDto>.Ok(notification.ToDto());
    }
    /// <summary>
    /// Marks all unread notifications for a user as read.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the number of notifications marked as read, or 0 if there were no unread notifications.
    /// </returns>
    public async Task<Result<int>> MarkAllNotificationsAsReadAsync(string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        CursorPaginationResponse<List<Notification>> unreadNotifications = await notificationRepository.GetUnreadNotificationsAsync( userId,int.MaxValue, null, dbContext, ct);
        if (!unreadNotifications.Data.Any())
        {
            return Result<int>.Ok(0);
        }

        DateTime now = DateTime.UtcNow;
        foreach (Notification notification in unreadNotifications.Data)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }
        
        IEnumerable<Task> tasks = unreadNotifications.Data.Select(imageRefreshService.RefreshNotificationImagesAsync);
        await Task.WhenAll(tasks);

        await dbContext.SaveChangesAsync(ct);
        return Result<int>.Ok(unreadNotifications.Data.Count);
    }
    /// <summary>
    /// Creates and sends a notification to a user via SignalR hub.
    /// </summary>
    /// <param name="notification">DTO containing the notification details to create and send.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the notification was successfully created and sent, or an error message if validation fails.
    /// </returns>
    /// <remarks>
    /// This method validates the notification, creates it in the database, and sends it to the receiver via SignalR
    /// if they have an active connection. The notification is sent to all active connections for the receiver.
    /// </remarks>
    public async Task<Result<bool>> SendNotificationAsync(CreateNotificationDto notification, CancellationToken ct = default)
    {
        ValidationResult? result = await createNotificationValidator.ValidateAsync(notification, ct);
        if (!result.IsValid)
        {
            string errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            return Result<bool>.Fail(errorMessage);
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        
        Notification notificationEntity = notification.ToEntity();
        
        notificationRepository.Add(notificationEntity, dbContext);
        await dbContext.SaveChangesAsync(ct);
        
        // Reload with Sender and Receiver for the DTO
        Notification? notificationWithUsers = await notificationRepository.GetByIdAsync(notificationEntity.Id, notification.ReceiverUserId, dbContext, ct);
        if (notificationWithUsers == null)
        {
            return Result<bool>.Fail("Failed to create notification.");
        }
        
        NotificationDto notificationDto = notificationWithUsers.ToDto();
        
        await SendNotificationToUserAsync(notification.ReceiverUserId, notificationDto, ct);
        
        return Result<bool>.Ok(true);
    }
    /// <summary>
    /// Sends a notification to a specific user via SignalR hub using their active connections.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to send the notification to.</param>
    /// <param name="notification">The notification DTO to send.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <remarks>
    /// This method retrieves all active SignalR connections for the user and sends the notification to all of them.
    /// If the user has no active connections, the notification is not sent (but it's still stored in the database).
    /// </remarks>
    private async Task SendNotificationToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default)
    {
        // Get user connections from the hub's static dictionary
        var userConnections = NotificationHub.GetUserConnectionsStatic(userId).ToList();
        if (userConnections.Any())
        {
            await hubContext.Clients.Clients(userConnections)
                .SendAsync("NotificationReceived", notification, ct);
        }
    }
}

