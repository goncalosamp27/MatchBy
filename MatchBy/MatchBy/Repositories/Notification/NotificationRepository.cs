using System.Linq.Dynamic.Core;
using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.Notification;

public class NotificationRepository: INotificationRepository
{
    private static IQueryable<Models.Notification> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .Notifications
            .Include(m => m.Sender)
            .Include(m => m.Receiver);
    }

    public async Task<CursorPaginationResponse<List<Models.Notification>>> GetNotificationsAsync(string userId, int pageSize, string? cursor, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        IQueryable<Models.Notification> query = Query(dbContext)
            .Where(m => m.ReceiverId == userId);

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(m => m.Id.CompareTo(cursor) < 0);
        }

        query = query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1);

        // Execute the query in a single operation
        List<Models.Notification> notifications = await query.ToListAsync(ct);
        
        bool hasNextPage = notifications.Count > pageSize;

        if (hasNextPage)
        {
            notifications.RemoveAt(notifications.Count - 1);
        }

        string? nextCursor = hasNextPage && notifications.Any()
            ? notifications[^1].Id
            : null;

        return new CursorPaginationResponse<List<Models.Notification>>
        {
            Data = notifications,
            NextCursor = nextCursor
        };
    }
    
    public async Task<CursorPaginationResponse<List<Models.Notification>>> GetUnreadNotificationsAsync(string userId, int pageSize, string? cursor, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        IQueryable<Models.Notification> query = Query(dbContext)
            .Where(n => n.ReceiverId == userId && !n.IsRead);

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(m => m.Id.CompareTo(cursor) < 0);
        }
        int take = pageSize >= int.MaxValue ? int.MaxValue : pageSize + 1;
        
        query = query
            .OrderByDescending(m => m.Id)
            .Take(take);

        // Execute the query in a single operation
        List<Models.Notification> notifications = await query.ToListAsync(ct);

        bool hasNextPage = notifications.Count > pageSize;

        if (hasNextPage)
        {
            notifications.RemoveAt(notifications.Count - 1);
        }

        string? nextCursor = hasNextPage && notifications.Any()
            ? notifications[0].Id
            : null;
    
        return new CursorPaginationResponse<List<Models.Notification>>
        {
            Data = notifications,
            NextCursor = nextCursor
        };
    }

    public Task<Models.Notification?> GetByIdAsync(string notificationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return Query(dbContext)
            .Where(n => n.Id == notificationId && n.ReceiverId == userId)
            .FirstOrDefaultAsync(ct);
    }

    public void Add(Models.Notification entity, ApplicationDbContext dbContext)
    {
        dbContext.Notifications.Add(entity);
    }

    public void Update(Models.Notification entity, ApplicationDbContext dbContext)
    {
        dbContext.Notifications.Update(entity);
    }

    public void Remove(Models.Notification entity, ApplicationDbContext dbContext)
    {
        dbContext.Notifications.Remove(entity);
    }
}