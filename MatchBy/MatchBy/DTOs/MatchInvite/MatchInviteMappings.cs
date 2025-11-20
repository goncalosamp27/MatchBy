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
            DeclinedAtUtc = matchInvite.DeclinedAtUtc,
            DeletedAtUtc = matchInvite.DeletedAtUtc
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

    public static void UpdateEntity(this Models.MatchInvite matchInvite, UpdateMatchInviteDto updateDto)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.Content))
        {
            matchInvite.Content = updateDto.Content;
        }

        if (updateDto.Status.HasValue)
        {
            matchInvite.Status = updateDto.Status.Value;
            
            // Set appropriate timestamp based on status
            if (updateDto.Status.Value == InviteStatus.Accepted)
            {
                matchInvite.AcceptedAtUtc = DateTime.UtcNow;
            }
            else if (updateDto.Status.Value == InviteStatus.Declined)
            {
                matchInvite.DeclinedAtUtc = DateTime.UtcNow;
            }
        }

        if (updateDto.ExpiresAtUtc.HasValue)
        {
            matchInvite.ExpiresAtUtc = updateDto.ExpiresAtUtc.Value;
        }

        matchInvite.UpdatedAtUtc = DateTime.UtcNow;
    }
}




