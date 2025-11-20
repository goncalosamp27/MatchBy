using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.DTOs.Team;

public sealed record TeamDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string OwnerId { get; init; }
    public UserDto? Owner { get; init; }
    public ICollection<UserDto> Members { get; init; }
    public string? ConversationId { get; init; }
    public ConversationDto? Conversation { get; init; }
    public int MaxMembers { get; init; }
    public string? ImageUrl { get; init; }
    public required TeamPrivacy Privacy { get; set; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}
