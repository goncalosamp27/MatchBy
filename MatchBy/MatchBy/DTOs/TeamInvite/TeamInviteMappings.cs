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
}



