using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;

namespace MatchBy.Services.TeamInvites;

public interface ITeamsInvitesService
{
    Task<Result<TeamInviteDto>> GetInviteById(string inviteId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<TeamInviteDto>>>> GetInvites(string teamId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<TeamInviteDto>> CreateInvite(CreateTeamInviteDto createDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteInvite(string inviteId, string userId, CancellationToken ct = default);
    Task<Result<TeamInviteDto>> AcceptInvite(string inviteId, string userId, CancellationToken ct = default);
}
