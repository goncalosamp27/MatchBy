using MatchBy.DTOs.Team;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.DTOs.TeamInvite;

public sealed record TeamInviteDto
{
    public required string Id { get; init; }
    public required string Content { get; init; }
    public required string SenderId { get; init; }
    public UserDto? Sender { get; init; }
    public required string ReceiverId { get; init; }
    public UserDto? Receiver { get; init; }
    public required string TeamId { get; init; }
    public TeamDto? Team { get; init; }
    public required InviteStatus Status { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public bool IsExpired { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? AcceptedAtUtc { get; init; }
    public DateTime? DeclinedAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}




