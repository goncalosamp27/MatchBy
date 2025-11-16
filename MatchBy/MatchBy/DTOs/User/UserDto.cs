namespace MatchBy.DTOs.User;

public sealed record UserDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
}
