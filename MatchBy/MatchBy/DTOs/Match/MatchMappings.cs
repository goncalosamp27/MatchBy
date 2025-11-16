using MatchBy.DTOs.User;
using MatchBy.Enums;

namespace MatchBy.DTOs.Match;

public static class MatchMappings
{
    public static MatchDto ToDto(this Models.Match match)
    {
        var list = new List<UserDto>();
        list.AddRange(match.Participants.Select(p => p.ToDto()));

        return new MatchDto
        {
            Id = match.Id,
            Location = match.Location,
            Address = match.Address,
            MatchDateTimeUtc = match.MatchDateTimeUtc,
            Description = match.Description,
            MinPlayers = match.minPlayers,
            MaxPlayers = match.maxPlayers,
            Sport = match.Sport,
            Status = match.Status,
            Privacy = match.Privacy,
            CreatorId = match.CreatorId,
            Creator = match.Creator?.ToDto(),
            ConversationId = match.ConversationId,
            Participants = list,
            CreatedAtUtc = match.CreatedAtUtc,
            UpdatedAtUtc = match.UpdatedAtUtc,
            DeletedAtUtc = match.DeletedAtUtc
        };
    }

    public static Models.Match ToEntity(this CreateMatchDto createMatchDto)
    {
        return new Models.Match
        {
            Id = $"match_{Guid.CreateVersion7()}",
            Location = createMatchDto.Location,
            Address = createMatchDto.Address,
            MatchDateTimeUtc = DateTime.SpecifyKind(createMatchDto.MatchDateTimeUtc, DateTimeKind.Utc),
            Description = createMatchDto.Description,
            minPlayers = createMatchDto.MinPlayers,
            maxPlayers = createMatchDto.MaxPlayers,
            Sport = createMatchDto.Sport,
            Status = MatchStatus.Pendent, // New matches are always created with Pendent status
            Privacy = createMatchDto.Privacy,
            CreatorId = createMatchDto.CreatorId,
            Participants = [],
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };
    }

    public static void UpdateEntity(this Models.Match match, UpdateMatchDto updateMatchDto)
    {
        match.Location = updateMatchDto.Location;
        match.Address = updateMatchDto.Address;
        match.MatchDateTimeUtc = DateTime.SpecifyKind(updateMatchDto.MatchDateTimeUtc, DateTimeKind.Utc);
        match.Description = updateMatchDto.Description;
        match.minPlayers = updateMatchDto.MinPlayers;
        match.maxPlayers = updateMatchDto.MaxPlayers;
        match.Sport = updateMatchDto.Sport;
        match.Privacy = updateMatchDto.Privacy;
        match.UpdatedAtUtc = DateTime.UtcNow;
    }
}
