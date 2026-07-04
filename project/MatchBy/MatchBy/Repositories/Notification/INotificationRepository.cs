using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.Notification;

public interface INotificationRepository
{
    Task<CursorPaginationResponse<List<Models.Notification>>> GetNotificationsAsync(string userId, int pageSize,
        string? cursor, ApplicationDbContext dbContext,
        CancellationToken ct = default);

    Task<CursorPaginationResponse<List<Models.Notification>>> GetUnreadNotificationsAsync(string userId, int pageSize,
        string? cursor, ApplicationDbContext dbContext,
        CancellationToken ct = default);
    Task<Models.Notification?> GetByIdAsync(string notificationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.Notification entity, ApplicationDbContext dbContext);
    void Update(Models.Notification entity, ApplicationDbContext dbContext);
    void Remove(Models.Notification entity, ApplicationDbContext dbContext);
}