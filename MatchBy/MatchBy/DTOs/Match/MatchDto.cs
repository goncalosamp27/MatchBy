using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.DTOs.Match;

public sealed record MatchDto
{
    public required string Id { get; init; }
    public required Location Location { get; init; }
    public required string Address { get; init; }
    public required DateTime MatchDateTimeUtc { get; init; }
    public required string Description { get; init; }
    public required int MinPlayers { get; init; }
    public required int MaxPlayers { get; init; }
    public required Sports Sport { get; init; }
    public required MatchStatus Status { get; init; }
    public required MatchPrivacy Privacy { get; init; }
    public required string CreatorId { get; init; }
    public UserDto? Creator { get; init; }
    public string? ConversationId { get; init; }
    public ICollection<UserDto> Participants { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}


