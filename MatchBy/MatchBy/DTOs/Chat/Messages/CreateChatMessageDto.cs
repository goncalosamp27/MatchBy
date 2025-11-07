namespace MatchBy.DTOs.Chat.Messages;

public sealed record CreateChatMessageDto
{
    public required string Content { get; init; }

    public required string CreatorUserId { get; init; }

    public required string ConversationId { get; init; }

    public string? ReplyToMessageId  { get; init; }
}
