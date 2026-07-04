using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.MatchInvite;

public class MatchInviteRepository: IMatchInviteRepository
{
    private static IQueryable<Models.MatchInvite> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .MatchInvites
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Match)
            .ThenInclude(m => m!.Creator)
            .Include(i => i.Match)
            .ThenInclude(m => m!.Participants);
    }

    public async Task<PaginationResponse<List<Models.MatchInvite>>> GetReceivedInvites(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.MatchInvite> query = Query(dbContext)
            .Where(i => i.ReceiverId == userId);

        int total = await query.CountAsync(ct);

        List<Models.MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.MatchInvite>>
        {
            Data = invites,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PaginationResponse<List<Models.MatchInvite>>> GetSentInvites(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.MatchInvite> query = Query(dbContext)
            .Where(i => i.SenderId == userId);

        int total = await query.CountAsync(ct);

        List<Models.MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.MatchInvite>>
        {
            Data = invites,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PaginationResponse<List<Models.MatchInvite>>> GetInvitesForMatch(string matchId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.MatchInvite> query = Query(dbContext)
            .Where(i => i.MatchId == matchId);

        int total = await query.CountAsync(ct);

        List<Models.MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.MatchInvite>>
        {
            Data = invites,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<Models.MatchInvite?> GetByIdAsync(string inviteId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext).FirstOrDefaultAsync(i => i.Id == inviteId, ct);
    }

    public async Task<Models.MatchInvite?> GetByIdAsync(string matchId, string receiverId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext).FirstOrDefaultAsync(i => i.MatchId == matchId && i.ReceiverId == receiverId, ct);
    }

    public void Add(Models.MatchInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.MatchInvites.Add(entity);
    }

    public void Update(Models.MatchInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.MatchInvites.Update(entity);
    }

    public void Remove(Models.MatchInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.MatchInvites.Remove(entity);
    }
}