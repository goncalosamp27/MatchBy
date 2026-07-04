namespace MatchBy.DTOs.Chat.Messages;

public sealed record UpdateChatMessageDto
{
    public required string ChatMessageId { get; init; }
    public required string Content { get; init; }
    public required string CreatorUserId { get; init; }
}
