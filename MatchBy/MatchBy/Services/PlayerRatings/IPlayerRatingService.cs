using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;

namespace MatchBy.Services.PlayerRatings;

public interface IPlayerRatingService
{
    Task<Result<PlayerRatingDto>> GetRatingById(string ratingId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsForMatch(string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsGivenByUser(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<PlayerRatingDto>>>> GetRatingsReceivedByUser(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PlayerRatingDto>> CreateRating(CreatePlayerRatingDto createDto, CancellationToken ct = default);
    Task<Result<PlayerRatingDto>> UpdateRating(UpdatePlayerRatingDto updateDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteRating(string ratingId, string userId, CancellationToken ct = default);
    Task<Result<double>> GetAverageRatingForUser(string userId, CancellationToken ct = default);
}




