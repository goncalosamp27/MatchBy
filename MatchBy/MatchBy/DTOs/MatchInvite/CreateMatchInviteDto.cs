namespace MatchBy.DTOs.MatchInvite;

public sealed record CreateMatchInviteDto
{
    public required string Content { get; init; }
    public required string SenderId { get; init; }
    public required string ReceiverId { get; init; }
    public required string MatchId { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
}
