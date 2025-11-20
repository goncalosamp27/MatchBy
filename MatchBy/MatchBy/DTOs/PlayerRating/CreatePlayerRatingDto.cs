namespace MatchBy.DTOs.PlayerRating;

public class CreatePlayerRatingDto
{

    public required string SentById { get; init; }
    public required string ReceivedById { get; init; }
    public required string MatchId { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    
}
