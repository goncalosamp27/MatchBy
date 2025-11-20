using System.Diagnostics.CodeAnalysis;

namespace MatchBy.DTOs.User;

public sealed record UserDto
{
    public UserDto()
    {
    }

    [SetsRequiredMembers]
    public UserDto(string id, string displayName, string? avatarUrl)
    {
        Id = id;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
}
