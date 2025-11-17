using MatchBy.DTOs.Team;
using MatchBy.Models;

namespace MatchBy.Services.Teams;

public interface ITeamService
{
    Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsAsync(string userId, int page, int pageSize, string q, CancellationToken ct = default);
    Task<Result<TeamDto>> GetTeamByIdAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto, CancellationToken ct = default);
    Task<Result<TeamDto>> UpdateTeamAsync(UpdateTeamDto updateTeamDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteTeamAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<int>> LeaveTeamAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<bool>> JoinTeamAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<bool>> SendTeamInviteAsync(string teamId, string senderId, string receiverId, string content, CancellationToken ct = default);
    Task<Result<bool>> DeleteTeamInviteAsync(string inviteId, string userId, CancellationToken ct = default);
}
