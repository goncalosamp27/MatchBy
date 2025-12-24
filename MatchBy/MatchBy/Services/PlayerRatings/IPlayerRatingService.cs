using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;

namespace MatchBy.Services.PlayerRatings;

public interface IPlayerRatingService
{
    Task<Result<PlayerRatingDto>> GetRatingByIdAsync(string ratingId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsForMatchAsync(string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsGivenByUserAsync(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsReceivedByUserAsync(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PlayerRatingDto>> CreateRatingAsync(CreatePlayerRatingDto createDto, CancellationToken ct = default);
    Task<Result<PlayerRatingDto>> UpdateRatingAsync(UpdatePlayerRatingDto updateDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteRatingAsync(string ratingId, string userId, CancellationToken ct = default);
}


