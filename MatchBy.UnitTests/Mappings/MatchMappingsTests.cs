using MatchBy.DTOs.Match;
using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.Mappings;

public class MatchMappingsTests
{
    [Fact]
    public void ToDto_WhenMatchIsValid_ShouldMapToDto()
    {
        // Arrange
        Match match = CreateValidMatch();

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.Equal(match.Id, dto.Id);
        Assert.Equal(match.Location, dto.Location);
        Assert.Equal(match.Address, dto.Address);
        Assert.Equal(match.MatchDateTimeUtc, dto.MatchDateTimeUtc);
        Assert.Equal(match.Description, dto.Description);
        Assert.Equal(match.MinPlayers, dto.MinPlayers);
        Assert.Equal(match.MaxPlayers, dto.MaxPlayers);
        Assert.Equal(match.Sport, dto.Sport);
        Assert.Equal(match.Status, dto.Status);
        Assert.Equal(match.Privacy, dto.Privacy);
        Assert.Equal(match.CreatorId, dto.CreatorId);
        Assert.Equal(match.ConversationId, dto.ConversationId);
        Assert.Equal(match.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(match.UpdatedAtUtc, dto.UpdatedAtUtc);
        Assert.NotNull(dto.Participants);
    }

    [Fact]
    public void ToDto_WhenCreatorIsNull_ShouldMapNullCreator()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Creator = null;

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.Null(dto.Creator);
    }

    [Fact]
    public void ToDto_WhenCreatorIsNotNull_ShouldMapCreator()
    {
        // Arrange
        Match match = CreateValidMatch();
        ApplicationUser creator = CreateValidUser();
        match.Creator = creator;

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.NotNull(dto.Creator);
        Assert.Equal(creator.Id, dto.Creator.Id);
    }

    [Fact]
    public void ToDto_WhenParticipantsIsEmpty_ShouldMapEmptyParticipantsList()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Participants = new List<ApplicationUser>();

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.NotNull(dto.Participants);
        Assert.Empty(dto.Participants);
    }

    [Fact]
    public void ToDto_WhenParticipantsHasMultipleUsers_ShouldMapAllParticipants()
    {
        // Arrange
        Match match = CreateValidMatch();
        ApplicationUser user1 = CreateValidUser("user1", "User One", "user1");
        ApplicationUser user2 = CreateValidUser("user2", "User Two", "user2");
        match.Participants = new List<ApplicationUser> { user1, user2 };

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.NotNull(dto.Participants);
        Assert.Equal(2, dto.Participants.Count);
        Assert.Contains(dto.Participants, p => p.Id == "user1");
        Assert.Contains(dto.Participants, p => p.Id == "user2");
    }

    [Fact]
    public void ToEntity_WhenCreateMatchDtoIsValid_ShouldMapToEntity()
    {
        // Arrange
        CreateMatchDto dto = CreateValidCreateMatchDto();

        // Act
        Match entity = dto.ToEntity();

        // Assert
        Assert.StartsWith("match_", entity.Id);
        Assert.Equal(dto.Location, entity.Location);
        Assert.Equal(dto.Address, entity.Address);
        Assert.Equal(DateTime.SpecifyKind(dto.MatchDateTimeUtc, DateTimeKind.Utc), entity.MatchDateTimeUtc);
        Assert.Equal(dto.Description, entity.Description);
        Assert.Equal(dto.MinPlayers, entity.MinPlayers);
        Assert.Equal(dto.MaxPlayers, entity.MaxPlayers);
        Assert.Equal(dto.Sport, entity.Sport);
        Assert.Equal(MatchStatus.Pendent, entity.Status);
        Assert.Equal(dto.Privacy, entity.Privacy);
        Assert.Equal(dto.CreatorId, entity.CreatorId);
        Assert.NotNull(entity.Participants);
        Assert.Empty(entity.Participants);
        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
    }

    [Fact]
    public void ToEntity_WhenMatchDateTimeUtcIsUtc_ShouldPreserveUtcKind()
    {
        // Arrange
        CreateMatchDto dto = CreateValidCreateMatchDto();
        DateTime utcDateTime = DateTime.UtcNow.AddDays(1);
        dto = dto with { MatchDateTimeUtc = utcDateTime };

        // Act
        Match entity = dto.ToEntity();

        // Assert
        Assert.Equal(DateTimeKind.Utc, entity.MatchDateTimeUtc.Kind);
        Assert.Equal(utcDateTime, entity.MatchDateTimeUtc);
    }

    [Fact]
    public void ToEntity_WhenMatchDateTimeUtcIsLocal_ShouldConvertToUtc()
    {
        // Arrange
        CreateMatchDto dto = CreateValidCreateMatchDto();
        DateTime localDateTime = DateTime.Now.AddDays(1);
        dto = dto with { MatchDateTimeUtc = localDateTime };

        // Act
        Match entity = dto.ToEntity();

        // Assert
        Assert.Equal(DateTimeKind.Utc, entity.MatchDateTimeUtc.Kind);
    }

    [Fact]
    public void ToEntity_StatusShouldAlwaysBePendent()
    {
        // Arrange
        CreateMatchDto dto = CreateValidCreateMatchDto();

        // Act
        Match entity = dto.ToEntity();

        // Assert
        Assert.Equal(MatchStatus.Pendent, entity.Status);
    }

    [Fact]
    public void UpdateEntity_WhenUpdateMatchDtoIsValid_ShouldUpdateEntity()
    {
        // Arrange
        Match match = CreateValidMatch();
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(updateDto.Location, match.Location);
        Assert.Equal(updateDto.Address, match.Address);
        Assert.Equal(DateTime.SpecifyKind(updateDto.MatchDateTimeUtc, DateTimeKind.Utc), match.MatchDateTimeUtc);
        Assert.Equal(updateDto.Description, match.Description);
        Assert.Equal(updateDto.MinPlayers, match.MinPlayers);
        Assert.Equal(updateDto.MaxPlayers, match.MaxPlayers);
        Assert.Equal(updateDto.Sport, match.Sport);
        Assert.Equal(updateDto.Privacy, match.Privacy);
        Assert.NotNull(match.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateEntity_WhenMatchDateTimeUtcIsUtc_ShouldPreserveUtcKind()
    {
        // Arrange
        Match match = CreateValidMatch();
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();
        DateTime utcDateTime = DateTime.UtcNow.AddDays(2);
        updateDto = updateDto with { MatchDateTimeUtc = utcDateTime };

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(DateTimeKind.Utc, match.MatchDateTimeUtc.Kind);
        Assert.Equal(utcDateTime, match.MatchDateTimeUtc);
    }

    [Fact]
    public void UpdateEntity_WhenMatchDateTimeUtcIsLocal_ShouldConvertToUtc()
    {
        // Arrange
        Match match = CreateValidMatch();
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();
        DateTime localDateTime = DateTime.Now.AddDays(2);
        updateDto = updateDto with { MatchDateTimeUtc = localDateTime };

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(DateTimeKind.Utc, match.MatchDateTimeUtc.Kind);
    }

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.UpdatedAtUtc = null;
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.NotNull(match.UpdatedAtUtc);
        Assert.NotEqual(default(DateTime), match.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeCreatedAtUtc()
    {
        // Arrange
        Match match = CreateValidMatch();
        DateTime originalCreatedAt = match.CreatedAtUtc;
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(originalCreatedAt, match.CreatedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeId()
    {
        // Arrange
        Match match = CreateValidMatch();
        string originalId = match.Id;
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(originalId, match.Id);
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeCreatorId()
    {
        // Arrange
        Match match = CreateValidMatch();
        string originalCreatorId = match.CreatorId;
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(originalCreatorId, match.CreatorId);
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeStatus()
    {
        // Arrange
        Match match = CreateValidMatch();
        MatchStatus originalStatus = match.Status;
        UpdateMatchDto updateDto = CreateValidUpdateMatchDto();

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(originalStatus, match.Status);
    }

    private static Match CreateValidMatch()
    {
        return new Match
        {
            Id = "match-id",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main Street",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinPlayers = 5,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator-id",
            ConversationId = null,
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    private static ApplicationUser CreateValidUser()
    {
        return new ApplicationUser
        {
            Id = "user-id",
            UserName = "testuser",
            DisplayName = "Test User"
        };
    }

    private static ApplicationUser CreateValidUser(string id, string displayName, string userName)
    {
        return new ApplicationUser
        {
            Id = id,
            DisplayName = displayName,
            UserName = userName
        };
    }

    private static CreateMatchDto CreateValidCreateMatchDto()
    {
        return new CreateMatchDto
        {
            Location = new Location(40.7128,
                -74.0060,
                "New York",
                "USA"),
            Address = "123 Main Street",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test match",
            MinPlayers = 5,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator-id",
            MinimumPlayersRating = MinimumPlayersAverage.All
        };
    }

    private static UpdateMatchDto CreateValidUpdateMatchDto()
    {
        return new UpdateMatchDto
        {
            UserId = "user-id",
            MatchId = "match-id",
            Location = new Location(40.7580,
                -73.9855,
                "New York",
                "USA"),
            Address = "456 Park Avenue",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2),
            Description = "Updated match description",
            MinPlayers = 6,
            MaxPlayers = 12,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Private,
            MinimumPlayersRating = MinimumPlayersAverage.All
        };
    }
}



