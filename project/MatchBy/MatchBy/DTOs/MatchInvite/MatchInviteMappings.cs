using MatchBy.DTOs.Match;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.DTOs.MatchInvite;

public static class MatchInviteMappings
{
    public static MatchInviteDto ToDto(this Models.MatchInvite matchInvite)
    {
        return new MatchInviteDto
        {
            Id = matchInvite.Id,
            Content = matchInvite.Content,
            SenderId = matchInvite.SenderId,
            Sender = matchInvite.Sender?.ToDto(),
            ReceiverId = matchInvite.ReceiverId,
            Receiver = matchInvite.Receiver?.ToDto(),
            MatchId = matchInvite.MatchId,
            Match = matchInvite.Match?.ToDto(),
            Status = matchInvite.Status,
            ExpiresAtUtc = matchInvite.ExpiresAtUtc,
            IsExpired = matchInvite.IsExpired,
            CreatedAtUtc = matchInvite.CreatedAtUtc,
            UpdatedAtUtc = matchInvite.UpdatedAtUtc,
            AcceptedAtUtc = matchInvite.AcceptedAtUtc,
        };
    }

    public static Models.MatchInvite ToEntity(this CreateMatchInviteDto createDto)
    {
        DateTime expiresAt = createDto.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(7); // Default 7 days expiration
        
        return new Models.MatchInvite
        {
            Id = $"matchinvite_{Guid.CreateVersion7()}",
            Content = createDto.Content,
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            MatchId = createDto.MatchId,
            Status = InviteStatus.Pending,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}



