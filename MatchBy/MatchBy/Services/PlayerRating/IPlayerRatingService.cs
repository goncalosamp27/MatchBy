using MatchBy.DTOs.PlayerRating;
using MatchBy.Models;
namespace MatchBy.Services.PlayerRating;




public interface IPlayerRatingService
{
    Task<Result<PlayerRatingDto>> CreatePlayerRatingAsync(CreatePlayerRatingDto request);
    
}
