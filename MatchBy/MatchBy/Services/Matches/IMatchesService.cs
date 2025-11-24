using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Services.Matches;

public interface IMatchesService
{
    Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchStatus? matchStatus, string? q, string? userId, int page = 1, int pageSize = 5, CancellationToken ct = default);
    Task<Result<MatchDto>> GetMatchById(string matchId, string? userId, CancellationToken ct = default);
    Task<Result<bool>> CreateMatch(CreateMatchDto createMatchDto, CancellationToken ct = default);
    Task<Result<bool>> UpdateMatch(UpdateMatchDto updateMatchDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteMatch(string matchId, string userId, CancellationToken ct = default);
    Task<Result<MatchDto>> JoinMatch(string matchId, string userId, CancellationToken ct = default);
    Task<Result<bool>> LeaveMatch(string matchId, string userId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesForUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesExceptUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesUserAttending(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default);
}

