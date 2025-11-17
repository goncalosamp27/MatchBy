using MatchBy.Models;

namespace MatchBy.DTOs.Team;

public sealed record CreateTeamDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string OwnerId { get; init; }
    public TeamPrivacy Privacy { get; set; }
    public required List<string> MembersIds { get; init; }
}
