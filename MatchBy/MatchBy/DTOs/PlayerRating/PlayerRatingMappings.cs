using MatchBy.DTOs.Match;
using MatchBy.DTOs.User;

namespace MatchBy.DTOs.PlayerRating;

public static class PlayerRatingMappings
{
    public static PlayerRatingDto ToDto(this Models.PlayerRating playerRating)
    {
        return new PlayerRatingDto
        {
            Id = playerRating.Id,
            Rating = playerRating.Rating,
            SentById = playerRating.SentById,
            SentBy = playerRating.SentBy?.ToDto(),
            ReceivedById = playerRating.ReceivedById,
            ReceivedBy = playerRating.ReceivedBy?.ToDto(),
            MatchId = playerRating.MatchId,
            Match = playerRating.Match?.ToDto(),
            CreatedAtUtc = playerRating.CreatedAtUtc,
            UpdatedAtUtc = playerRating.UpdatedAtUtc,
            DeletedAtUtc = playerRating.DeletedAtUtc
        };
    }

    public static Models.PlayerRating ToEntity(this CreatePlayerRatingDto createDto)
    {
        return new Models.PlayerRating
        {
            Id = $"playerrating_{Guid.CreateVersion7()}",
            Rating = createDto.Rating,
            SentById = createDto.SentById,
            ReceivedById = createDto.ReceivedById,
            MatchId = createDto.MatchId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this Models.PlayerRating playerRating, UpdatePlayerRatingDto updateDto)
    {
        playerRating.Rating = updateDto.Rating;
        playerRating.UpdatedAtUtc = DateTime.UtcNow;
    }
}



