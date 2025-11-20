using MatchBy.DTOs.Team;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.DTOs.TeamInvite;

public static class TeamInviteMappings
{
    public static TeamInviteDto ToDto(this Models.TeamInvite teamInvite)
    {
        return new TeamInviteDto
        {
            Id = teamInvite.Id,
            Content = teamInvite.Content,
            SenderId = teamInvite.SenderId,
            Sender = teamInvite.Sender?.ToDto(),
            ReceiverId = teamInvite.ReceiverId,
            Receiver = teamInvite.Receiver?.ToDto(),
            TeamId = teamInvite.TeamId,
            Team = teamInvite.Team?.ToDto(),
            Status = teamInvite.Status,
            ExpiresAtUtc = teamInvite.ExpiresAtUtc,
            IsExpired = teamInvite.IsExpired,
            CreatedAtUtc = teamInvite.CreatedAtUtc,
            UpdatedAtUtc = teamInvite.UpdatedAtUtc,
            AcceptedAtUtc = teamInvite.AcceptedAtUtc,
            DeclinedAtUtc = teamInvite.DeclinedAtUtc,
            DeletedAtUtc = teamInvite.DeletedAtUtc
        };
    }

    public static Models.TeamInvite ToEntity(this CreateTeamInviteDto createDto)
    {
        DateTime expiresAt = createDto.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(7); // Default 7 days expiration
        
        return new Models.TeamInvite
        {
            Id = $"teaminvite_{Guid.CreateVersion7()}",
            Content = createDto.Content,
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            TeamId = createDto.TeamId,
            Status = InviteStatus.Pending,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this Models.TeamInvite teamInvite, UpdateTeamInviteDto updateDto)
    {
        if (!string.IsNullOrWhiteSpace(updateDto.Content))
        {
            teamInvite.Content = updateDto.Content;
        }

        if (updateDto.Status.HasValue)
        {
            teamInvite.Status = updateDto.Status.Value;
            
            // Set appropriate timestamp based on status
            if (updateDto.Status.Value == InviteStatus.Accepted)
            {
                teamInvite.AcceptedAtUtc = DateTime.UtcNow;
            }
            else if (updateDto.Status.Value == InviteStatus.Declined)
            {
                teamInvite.DeclinedAtUtc = DateTime.UtcNow;
            }
        }

        if (updateDto.ExpiresAtUtc.HasValue)
        {
            teamInvite.ExpiresAtUtc = updateDto.ExpiresAtUtc.Value;
        }

        teamInvite.UpdatedAtUtc = DateTime.UtcNow;
    }
}




