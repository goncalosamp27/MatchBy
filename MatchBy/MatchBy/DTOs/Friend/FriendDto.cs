using MatchBy.DTOs.User;

namespace MatchBy.DTOs.Friend;

public sealed record FriendDto
{
    public required string Id { get; init; }
    public required string SenderId { get; init; }
    public UserDto? Sender { get; init; }
    public required string ReceiverId { get; init; }
    public UserDto? Receiver { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}