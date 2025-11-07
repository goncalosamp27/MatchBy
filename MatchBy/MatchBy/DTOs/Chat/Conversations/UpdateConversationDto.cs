using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.DTOs.Chat.Conversations;

public sealed record UpdateConversationDto
{
    public required string ConversationId { get; init; }
    public required string CreatorUserId { get; init; }
    public required List<string> ParticipantIds { get; init; }
    public  IBrowserFile?  File { get; init; }
}
