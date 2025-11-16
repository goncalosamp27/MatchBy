using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Conversations;

public sealed record ConversationDto
{
    public required string Id { get; init; }
    public required ConversationType Type { get; init; }
    public string? Title { get; init; }
    
    public string? ImageUrl { get; init; }
    public required string CreatorId { get; init; }

    public string? TeamId { get; init; }
    public string? MatchId { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    
    public required long MessagesCount { get; init; }
    public string? LastMessageContent { get; init; }
    public DateTime? LastMessageAtUtc { get; init; }
    
    public List<ConversationParticipantDto> Participants { get; init; }
}

public sealed record ConversationParticipantDto
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Username { get; init; }
    public string? ImageUrl { get; init; }
}
