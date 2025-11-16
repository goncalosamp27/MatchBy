using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Messages;

public sealed record ChatMessageDto
{
    public required string Id { get; init; }
    public string? Content { get; init; }
    public required string SenderId { get; init; }
    public UserDto Sender { get; init; }
    public Location? Location { get; init; }
    public string? InviteUrl { get; init; }
    public string? ReplyToMessageId { get; init; }
    public ChatMessageDto? ReplyToMessage { get; init; }
    public required string ConversationId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}
