using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.User;

namespace MatchBy.DTOs.Team;

public static class TeamMappings
{
    public static TeamDto ToDto(this Models.Team team)
    {
        var list = new List<UserDto>();
        list.AddRange(team.Members.Select(p => p.ToDto()));

        return new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            OwnerId = team.OwnerId,
            Owner = team.Owner?.ToDto(),
            Members = list,
            ConversationId = team.ConversationId,
            Conversation = team.Conversation?.ToDto(),
            Privacy = team.Privacy,
            CreatedAtUtc = team.CreatedAtUtc,
            UpdatedAtUtc = team.UpdatedAtUtc,
            DeletedAtUtc = team.DeletedAtUtc
        };
    }

    public static Models.Team ToEntity(this CreateTeamDto createTeamDto)
    {
        return new Models.Team
        {
            Id = $"team_{Guid.CreateVersion7()}",
            Name = createTeamDto.Name,
            Description = createTeamDto.Description,
            OwnerId = createTeamDto.OwnerId,
            Privacy = createTeamDto.Privacy,
            Members = [],
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };
    }

    public static void UpdateEntity(this Models.Team team, UpdateTeamDto updateTeamDto)
    {
        team.Name = updateTeamDto.Name;
        team.Description = updateTeamDto.Description;
        team.UpdatedAtUtc = DateTime.UtcNow;
        team.Privacy = updateTeamDto.Privacy;
    }
}
