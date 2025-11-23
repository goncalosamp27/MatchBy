using MatchBy.DTOs.MatchInvite;
using MatchBy.Models;

namespace MatchBy.Services.MatchInvites;

public interface IMatchesInvitesService
{
    Task<Result<MatchInviteDto>> GetInviteById(string inviteId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetReceivedInvites(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetSentInvites(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetInvitesForMatch(string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<MatchInviteDto>> CreateInvite(CreateMatchInviteDto createDto, CancellationToken ct = default);
    Task<Result<MatchInviteDto>> UpdateInvite(UpdateMatchInviteDto updateDto, string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteInvite(string inviteId, string userId, CancellationToken ct = default);
    Task<Result<MatchInviteDto>> AcceptInvite(string inviteId, string userId, CancellationToken ct = default);
    Task<Result<MatchInviteDto>> DeclineInvite(string inviteId, string userId, CancellationToken ct = default);
    Task<Result<MatchInviteDto>> CancelInvite(string inviteId, string userId, CancellationToken ct = default);
}
