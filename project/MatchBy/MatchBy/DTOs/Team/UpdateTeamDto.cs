using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.DTOs.Team;

public sealed record UpdateTeamDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required int MaxMembers { get; init; }
    public required string OwnerId { get; init; }
    public required List<string> MembersIds { get; init; }
    public TeamPrivacy Privacy { get; init; }
    public IBrowserFile?  File { get; init; }
}
