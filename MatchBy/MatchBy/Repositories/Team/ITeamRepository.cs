using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.Models;

namespace MatchBy.Repositories.Team;

public interface ITeamRepository
{
    Task<Models.Team?> GetByIdAsync(string teamId, string userId, bool hasInvite, ApplicationDbContext dbContext, CancellationToken ct = default);

    Task<Models.Team?> GetTeamUserOwnsByIdAsync(string teamId, string ownerId, ApplicationDbContext dbContext, CancellationToken ct = default);

    Task<Models.Team?> GetTeamUserParticipatesByIdAsync(string teamId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.Team>>> GetTeamsAsync(TeamQueryParametersDto teamQueryParametersDto, List<string> userPendingInvites, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.Team>>> GetAvailableTeamsAsync(TeamQueryParametersDto teamQueryParametersDto, List<string> userPendingInvites,ApplicationDbContext dbContext, CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Team>>> GetTeamsUserOwnAsync(string userId, int page, int pageSize, string q, ApplicationDbContext dbContext, CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Team>>> GetTeamsUserParticipateAsync(string userId, int page, int pageSize, string q, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.Team entity, ApplicationDbContext dbContext);
    void Update(Models.Team entity, ApplicationDbContext dbContext);
    void Remove(Models.Team entity, ApplicationDbContext dbContext);
}