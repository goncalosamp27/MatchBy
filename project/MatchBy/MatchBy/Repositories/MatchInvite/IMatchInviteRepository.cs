using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.MatchInvite;

public interface IMatchInviteRepository
{
    Task<PaginationResponse<List<Models.MatchInvite>>> GetReceivedInvites(
        string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.MatchInvite>>> GetSentInvites(
        string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.MatchInvite>>> GetInvitesForMatch(
        string matchId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Models.MatchInvite?> GetByIdAsync(string inviteId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<Models.MatchInvite?> GetByIdAsync(string matchId, string receiverId, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.MatchInvite entity, ApplicationDbContext dbContext);
    void Update(Models.MatchInvite entity, ApplicationDbContext dbContext);
    void Remove(Models.MatchInvite entity, ApplicationDbContext dbContext);
}