namespace MatchBy.DTOs.TeamInvite;

public sealed record CreateTeamInviteDto
{
    public required string Content { get; init; }
    public required string SenderId { get; init; }
    public required string ReceiverId { get; init; }
    public required string TeamId { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}



