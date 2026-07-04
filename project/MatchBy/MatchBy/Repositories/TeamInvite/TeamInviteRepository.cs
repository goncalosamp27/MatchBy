using System.Linq.Dynamic.Core;
using MatchBy.Data;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.TeamInvite;

public class TeamInviteRepository : ITeamInviteRepository
{
    public async Task<Models.TeamInvite?> GetByIdAsync(string inviteId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext).Where(i => i.Id == inviteId).FirstOrDefaultAsync(ct);
    }

    private static IQueryable<Models.TeamInvite> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .TeamInvites
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Team)
            .ThenInclude(t => t!.Owner)
            .Include(i => i.Team)
            .ThenInclude(t => t!.Members);
    }

    public async Task<bool> ExistsPendingInviteByTeamAndUser(string teamId, string userId, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        return await dbContext.TeamInvites
            .AnyAsync(i => i.TeamId == teamId 
                           && i.ReceiverId == userId 
                           && i.Status == InviteStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow, ct);
    }

    public async Task<PaginationResponse<List<Models.TeamInvite>>> GetInvites(string teamId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10,
        CancellationToken ct = default)
    {
        IQueryable<Models.TeamInvite> query = Query(dbContext)
            .Where(i => i.TeamId == teamId);

        int total = await query.CountAsync(ct);

        List<Models.TeamInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.TeamInvite>>
        {
            Data = invites,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public void Add(Models.TeamInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.TeamInvites.Add(entity);
    }

    public void Update(Models.TeamInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.TeamInvites.Update(entity);
    }

    public void Remove(Models.TeamInvite entity, ApplicationDbContext dbContext)
    {
        dbContext.TeamInvites.Remove(entity);
    }
}