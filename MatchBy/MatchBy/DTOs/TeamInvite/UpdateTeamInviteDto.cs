using MatchBy.Models;

namespace MatchBy.DTOs.TeamInvite;

public sealed record UpdateTeamInviteDto
{
    public required string Id { get; init; }
    public string? Content { get; init; }
    public InviteStatus? Status { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
