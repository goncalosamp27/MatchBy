using MatchBy.Models;

namespace MatchBy.DTOs.MatchInvite;

public sealed record UpdateMatchInviteDto
{
    public required string Id { get; init; }
    public string? Content { get; init; }
    public InviteStatus? Status { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}