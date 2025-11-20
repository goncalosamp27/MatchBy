namespace MatchBy.DTOs.PlayerRating;

public static class PlayerRatingMappings
{
    public static PlayerRatingDto ToDto(this Models.PlayerRating rating)
    {
        return new PlayerRatingDto
        {
            Id = rating.Id,
            SentById = rating.SentById,
            ReceivedById = rating.ReceivedById,
            MatchId = rating.MatchId,
            Rating = (int)rating.Rating,
            Comment = rating.Comment,
            CreatedAtUtc = rating.CreatedAtUtc
        };
    }

    public static Models.PlayerRating ToEntity(this CreatePlayerRatingDto dto)
    {
        return new Models.PlayerRating
        {
            Id = Guid.NewGuid().ToString(),
            SentById = dto.SentById,
            ReceivedById = dto.ReceivedById,
            MatchId = dto.MatchId,
            Rating = dto.Rating,
            Comment = string.IsNullOrWhiteSpace(dto.Comment) ? null : dto.Comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

