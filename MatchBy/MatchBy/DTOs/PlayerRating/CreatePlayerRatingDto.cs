namespace MatchBy.DTOs.PlayerRating;

public sealed record CreatePlayerRatingDto
{
    public required float Rating { get; init; }
    public required string SentById { get; init; }
    public required string ReceivedById { get; init; }
    public required string MatchId { get; init; }
}



