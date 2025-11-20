namespace MatchBy.DTOs.Friend;

public sealed record CreateFriendDto
{
    public required string SenderId { get; init; }
    public required string ReceiverId { get; init; }
}



