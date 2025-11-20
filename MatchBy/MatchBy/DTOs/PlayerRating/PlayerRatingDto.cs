namespace MatchBy.DTOs.PlayerRating;

public sealed record PlayerRatingDto
{
    public required string Id { get; init; }
    public required string SentById { get; init; }
    public required string ReceivedById { get; init; }
    public required string MatchId { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

