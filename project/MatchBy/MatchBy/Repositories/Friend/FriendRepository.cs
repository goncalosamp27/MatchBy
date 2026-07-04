using MatchBy.Data;
using MatchBy.DTOs.Friend;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.Friend;

public class FriendRepository: IFriendRepository
{
    private static IQueryable<Models.Friend> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .Friends
            .Include(f => f.Sender)
            .Include(f => f.Receiver);
    }
    public async Task<PaginationResponse<List<Models.Friend>>> GetUserFriends(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.Friend> query = Query(dbContext)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == FriendStatus.Accepted);

        int total = await query.CountAsync(ct);

        List<Models.Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.Friend>>
        {
            Data = friends,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PaginationResponse<List<Models.Friend>>> GetFriendRequestsSent(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.Friend> query = Query(dbContext)
            .Where(f => f.SenderId == userId && f.Status == FriendStatus.Pending);

        int total = await query.CountAsync(ct);

        List<Models.Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.Friend>>
        {
            Data = friends,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PaginationResponse<List<Models.Friend>>> GetFriendRequestsReceived(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.Friend> query = Query(dbContext)
            .Where(f => f.ReceiverId == userId && f.Status == FriendStatus.Pending);

        int total = await query.CountAsync(ct);

        List<Models.Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.Friend>>
        {
            Data = friends,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<Models.Friend?> GetByIdAsync(string friendshipId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
       return Query(dbContext)
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);
    }

    public async Task<Models.Friend?> ExistsAsync(string user1, string user2, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext)
            .FirstOrDefaultAsync(f => 
                (f.SenderId == user1 && f.ReceiverId == user2) || 
                (f.SenderId == user2 && f.ReceiverId == user1), ct);
    }

    public void Add(Models.Friend entity, ApplicationDbContext dbContext)
    {
        dbContext.Friends.Add(entity);
    }

    public void Update(Models.Friend entity, ApplicationDbContext dbContext)
    {
        dbContext.Friends.Update(entity);
    }

    public void Remove(Models.Friend entity, ApplicationDbContext dbContext)
    {
        dbContext.Friends.Remove(entity);
    }
}