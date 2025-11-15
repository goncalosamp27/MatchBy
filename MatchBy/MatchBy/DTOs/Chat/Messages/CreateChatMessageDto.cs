using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Messages;

public sealed record CreateChatMessageDto
{
    public string? Content { get; init; }
    public required string CreatorUserId { get; init; }
    public required string ConversationId { get; init; }
    public string? ReplyToMessageId  { get; init; }
    public Location? Location { get; init; }    
    public string? InviteUrl { get; init; }
}
