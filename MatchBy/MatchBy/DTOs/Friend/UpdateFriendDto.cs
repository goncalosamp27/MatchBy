namespace MatchBy.DTOs.Friend;

public sealed record UpdateFriendDto
{
    public required string Id { get; init; }
    // Friend relationships typically don't need many update operations
    // This is a placeholder for future extensibility
}



