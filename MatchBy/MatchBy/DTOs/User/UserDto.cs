using MatchBy.Enums;

namespace MatchBy.DTOs.User;

public sealed record UserDto
{
    public required string Id { get; init; }
    public required string UserName { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
    public float PlayerRating { get; init; }
    public List<Sports> PreferredSports { get; init; } = [];
    public int JoinedMatchesCount { get; init; }
    public string? Bio { get; init; }

}
