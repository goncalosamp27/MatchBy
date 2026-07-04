using MatchBy.DTOs.Notification;
using MatchBy.Models;

namespace MatchBy.Services.Notifications;

public interface INotificationService
{
    Task<Result<CursorPaginationResponse<List<NotificationDto>>>> GetNotifications(int pageSize, string userId,string? cursor, CancellationToken ct = default);
    Task<Result<NotificationDto>> ReadNotification(string notificationId, string userId, CancellationToken ct = default);
    Task<Result<int>> MarkAllNotificationsAsReadAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> SendNotificationAsync(CreateNotificationDto notification, CancellationToken ct = default);
}


