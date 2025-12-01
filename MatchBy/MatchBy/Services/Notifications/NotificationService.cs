using FluentValidation;
using FluentValidation.Results;
using MatchBy.DTOs.Notification;
using Microsoft.AspNetCore.SignalR;
using MatchBy.Hubs;
using Microsoft.EntityFrameworkCore;
using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;

namespace MatchBy.Services.Notifications;

public class NotificationService(
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
        
        // Build the query first
        IQueryable<Notification> query = dbContext
            .Notifications
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.ReceiverId == userId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(m => m.Id.CompareTo(cursor) < 0);
        }

        query = query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1);

        // Execute the query in a single operation
        List<Notification> notifications = await query.ToListAsync(ct);

        bool hasNextPage = notifications.Count > pageSize;

        if (hasNextPage)
        {
            notifications.RemoveAt(notifications.Count - 1);
        }

        IEnumerable<Task> tasks = notifications.Select(imageRefreshService.RefreshNotificationImagesAsync);
        await Task.WhenAll(tasks);
        
        var notificationsDtos = notifications.Select(m => m.ToDto()).ToList();

        string? nextCursor = hasNextPage && notificationsDtos.Any()
            ? notificationsDtos[0].Id
            : null;
    
        return Result<CursorPaginationResponse<List<NotificationDto>>>.Ok(
            new CursorPaginationResponse<List<NotificationDto>>
            {
                Data = notificationsDtos,
                NextCursor = nextCursor
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

        Notification? notification = await dbContext
            .Notifications
            .Include(n => n.Sender)
            .Include(n => n.Receiver)
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReceiverId == userId, ct);

        if (notification == null)
        {
            return Result<NotificationDto>.Fail("Notification not found.");
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTime.UtcNow;

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

        List<Notification> unreadNotifications = await dbContext
            .Notifications
            .Where(n => n.ReceiverId == userId && !n.IsRead)
            .ToListAsync(ct);

        if (!unreadNotifications.Any())
        {
            return Result<int>.Ok(0);
        }

        DateTime now = DateTime.UtcNow;
        foreach (Notification notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }

        await dbContext.SaveChangesAsync(ct);
        return Result<int>.Ok(unreadNotifications.Count);
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
            string errorMessage = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
            return Result<bool>.Fail(errorMessage);
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        
        Notification notificationEntity = notification.ToEntity();
        
        await dbContext.Notifications.AddAsync(notificationEntity, ct);
        await dbContext.SaveChangesAsync(ct);
        
        // Reload with Sender and Receiver for the DTO
        Notification? notificationWithUsers = await dbContext.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Receiver)
            .FirstOrDefaultAsync(n => n.Id == notificationEntity.Id, ct);
        
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
    
    /// <summary>
    /// Sends a notification to multiple users via SignalR hub using their active connections.
    /// </summary>
    /// <param name="userIds">Collection of user IDs to send the notification to.</param>
    /// <param name="notification">The notification DTO to send.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <remarks>
    /// This method retrieves all active SignalR connections for all specified users and sends the notification
    /// to all of them. Duplicate connections are removed before sending.
    /// </remarks>
    private async Task SendNotificationToUsersAsync(IEnumerable<string> userIds, NotificationDto notification, CancellationToken ct = default)
    {
        // Get all connections for the specified users
        var allConnections = userIds
            .SelectMany(NotificationHub.GetUserConnectionsStatic)
            .Distinct()
            .ToList();

        if (allConnections.Any())
        {
            await hubContext.Clients.Clients(allConnections)
                .SendAsync("NotificationReceived", notification, ct);
        }
    }
}

