namespace MatchBy.DTOs.PlayerRating;

public sealed record UpdatePlayerRatingDto
{
    public required string Id { get; init; }
    public required float Rating { get; init; }
    public required string SentById { get; init; }
}




