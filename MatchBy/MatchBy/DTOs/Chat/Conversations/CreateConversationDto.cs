using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Conversations;

public sealed record CreateConversationDto
{
    public required string CreatorUserId { get; init; }

    public required ConversationType ConversationType { get; init; }

    public required List<string> ParticipantIds { get; init; }

    public string? Title { get; init; }

    public string? TeamId { get; init; }

    public string? MatchId { get; init; }
}
