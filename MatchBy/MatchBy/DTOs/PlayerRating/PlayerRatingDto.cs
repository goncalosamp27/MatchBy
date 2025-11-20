using MatchBy.DTOs.Match;
using MatchBy.DTOs.User;

namespace MatchBy.DTOs.PlayerRating;

public sealed record PlayerRatingDto
{
    public required string Id { get; init; }
    public required int Rating { get; init; }
    public required string SentById { get; init; }
    public UserDto? SentBy { get; init; }
    public required string ReceivedById { get; init; }
    public UserDto? ReceivedBy { get; init; }
    public required string MatchId { get; init; }
    public MatchDto? Match { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}




