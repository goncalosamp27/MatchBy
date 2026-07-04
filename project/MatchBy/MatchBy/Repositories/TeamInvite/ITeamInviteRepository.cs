using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.TeamInvite;

public interface ITeamInviteRepository
{
    Task<bool> ExistsPendingInviteByTeamAndUser(string teamId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.TeamInvite>>> GetInvites(string teamId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Models.TeamInvite?> GetByIdAsync(string inviteId, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.TeamInvite entity, ApplicationDbContext dbContext);
    void Update(Models.TeamInvite entity, ApplicationDbContext dbContext);
    void Remove(Models.TeamInvite entity, ApplicationDbContext dbContext);
}