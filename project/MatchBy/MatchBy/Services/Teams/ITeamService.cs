using MatchBy.DTOs.Team;
using MatchBy.Models;

namespace MatchBy.Services.Teams;

public interface ITeamService
{
    Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsAsync(TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<TeamDto>>>> GetAvailableTeamsAsync(TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserOwnAsync(string userId, int page, int pageSize, string q, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserParticipateAsync(string userId, int page, int pageSize, string q, CancellationToken ct = default);
    Task<Result<TeamDto>> GetTeamByIdAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto, CancellationToken ct = default);
    Task<Result<TeamDto>> UpdateTeamAsync(UpdateTeamDto updateTeamDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteTeamAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteTeamImageAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<int>> LeaveTeamAsync(string teamId, string userId, CancellationToken ct = default);
    Task<Result<bool>> JoinTeamAsync(string teamId, string userId, CancellationToken ct = default);
}
