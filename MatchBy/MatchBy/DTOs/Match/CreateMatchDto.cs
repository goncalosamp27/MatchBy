using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.DTOs.Match;

public sealed record CreateMatchDto
{
    public required Location Location { get; init; }
    public required string Address { get; init; }
    public required DateTime MatchDateTimeUtc { get; init; }
    public required string Description { get; init; }
    public required MinimumPlayersAverage MinimumPlayersRating { get; init; }
    public required int MinPlayers { get; init; }
    public required int MaxPlayers { get; init; }
    public required Sports Sport { get; init; }
    public required MatchPrivacy Privacy { get; init; }
    public required string CreatorId { get; init; }
    public List<string> MembersIds { get; init; } = [];
}
