namespace MatchBy.DTOs.Chat.Messages;

public sealed record ChatMessageDto
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required string SenderId { get; init; }
    public UserSummaryDto Sender { get; init; }
    public string? ReplyToMessageId { get; init; }
    public ChatMessageDto? ReplyToMessage { get; init; }
    public required string ConversationId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}
public sealed record UserSummaryDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }
}

