using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Match;

public class MatchMappingsTests
{
    #region ToDto Tests

    [Fact]
    public void ToDto_WithCompleteMatch_ShouldMapAllProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        DateTime updatedAt = DateTime.UtcNow.AddDays(1);
        DateTime matchDateTime = DateTime.UtcNow.AddDays(7);
        var location = new Location(40.7128, -74.0060, "New York", "USA");

        var creator = new ApplicationUser
        {
            Id = "creator_123",
            UserName = "creator",
            DisplayName = "Match Creator",
            CreatedAtUtc = createdAt
        };

        var participant1 = new ApplicationUser
        {
            Id = "participant_456",
            UserName = "participant1",
            DisplayName = "Participant One",
            CreatedAtUtc = createdAt
        };

        var match = new Models.Match
        {
            Id = "match_789",
            Location = location,
            Address = "123 Main St, New York, NY",
            MatchDateTimeUtc = matchDateTime,
            Description = "Friendly football match",
            minPlayers = 4,
            maxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123",
            Creator = creator,
            ConversationId = "conv_999",
            Participants = new List<ApplicationUser> { creator, participant1 },
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            DeletedAtUtc = null
        };

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("match_789", dto.Id);
        Assert.Equal(location, dto.Location);
        Assert.Equal("123 Main St, New York, NY", dto.Address);
        Assert.Equal(matchDateTime, dto.MatchDateTimeUtc);
        Assert.Equal("Friendly football match", dto.Description);
        Assert.Equal(4, dto.MinPlayers);
        Assert.Equal(10, dto.MaxPlayers);
        Assert.Equal(Sports.Football, dto.Sport);
        Assert.Equal(MatchStatus.Pendent, dto.Status);
        Assert.Equal(MatchPrivacy.Public, dto.Privacy);
        Assert.Equal("creator_123", dto.CreatorId);
        Assert.NotNull(dto.Creator);
        Assert.Equal("creator_123", dto.Creator.Id);
        Assert.Equal("conv_999", dto.ConversationId);
        Assert.Equal(2, dto.Participants.Count);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void ToDto_WithNullOptionalProperties_ShouldMapCorrectly()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        var location = new Location(0, 0, "City", "Country");

        var match = new Models.Match
        {
            Id = "match_789",
            Location = location,
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            minPlayers = 2,
            maxPlayers = 5,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Private,
            CreatorId = "creator_123",
            Creator = null,
            ConversationId = null,
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        MatchDto dto = match.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Null(dto.Creator);
        Assert.Null(dto.ConversationId);
        Assert.Empty(dto.Participants);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    #endregion

    #region ToEntity Tests

    [Fact]
    public void ToEntity_WithCreateMatchDto_ShouldMapAllProperties()
    {
        // Arrange
        var location = new Location(40.7128, -74.0060, "New York", "USA");
        DateTime matchDateTime = DateTime.UtcNow.AddDays(7);

        var createDto = new CreateMatchDto
        {
            Location = location,
            Address = "123 Main St, New York, NY",
            MatchDateTimeUtc = matchDateTime,
            Description = "Friendly football match",
            MinPlayers = 4,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123"
        };

        // Act
        Models.Match entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("match_", entity.Id);
        Assert.Equal(location, entity.Location);
        Assert.Equal("123 Main St, New York, NY", entity.Address);
        Assert.Equal(DateTimeKind.Utc, entity.MatchDateTimeUtc.Kind);
        Assert.Equal("Friendly football match", entity.Description);
        Assert.Equal(4, entity.minPlayers);
        Assert.Equal(10, entity.maxPlayers);
        Assert.Equal(Sports.Football, entity.Sport);
        Assert.Equal(MatchStatus.Pendent, entity.Status);
        Assert.Equal(MatchPrivacy.Public, entity.Privacy);
        Assert.Equal("creator_123", entity.CreatorId);
        Assert.NotNull(entity.Participants);
        Assert.Empty(entity.Participants);
        Assert.NotEqual(DateTime.MinValue, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void ToEntity_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Arrange
        var createDto = new CreateMatchDto
        {
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 5,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123"
        };

        // Act
        Models.Match entity1 = createDto.ToEntity();
        Models.Match entity2 = createDto.ToEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void ToEntity_ShouldSetStatusToPendent()
    {
        // Arrange
        var createDto = new CreateMatchDto
        {
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 5,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123"
        };

        // Act
        Models.Match entity = createDto.ToEntity();

        // Assert
        Assert.Equal(MatchStatus.Pendent, entity.Status);
    }

    [Fact]
    public void ToEntity_ShouldInitializeEmptyParticipantsCollection()
    {
        // Arrange
        var createDto = new CreateMatchDto
        {
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 5,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123"
        };

        // Act
        Models.Match entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity.Participants);
        Assert.Empty(entity.Participants);
    }

    #endregion

    #region UpdateEntity Tests

    [Fact]
    public void UpdateEntity_WithUpdateMatchDto_ShouldUpdateProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow.AddDays(-10);
        var oldLocation = new Location(0, 0, "Old City", "Old Country");
        var newLocation = new Location(40.7128, -74.0060, "New York", "USA");

        var match = new Models.Match
        {
            Id = "match_123",
            Location = oldLocation,
            Address = "Old Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(5),
            Description = "Old Description",
            minPlayers = 2,
            maxPlayers = 5,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Private,
            CreatorId = "creator_123",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateMatchDto
        {
            UserId = "creator_123",
            MatchId = "match_123",
            Location = newLocation,
            Address = "New Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(7),
            Description = "New Description",
            MinPlayers = 4,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public
        };

        // Act
        DateTime beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        match.UpdateEntity(updateDto);
        DateTime afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.Equal(newLocation, match.Location);
        Assert.Equal("New Address", match.Address);
        Assert.Equal("New Description", match.Description);
        Assert.Equal(4, match.minPlayers);
        Assert.Equal(10, match.maxPlayers);
        Assert.Equal(Sports.Football, match.Sport);
        Assert.Equal(MatchPrivacy.Public, match.Privacy);
        Assert.NotNull(match.UpdatedAtUtc);
        Assert.True(match.UpdatedAtUtc >= beforeUpdate && match.UpdatedAtUtc <= afterUpdate);
        Assert.Equal(createdAt, match.CreatedAtUtc); // Should not change
        Assert.Equal("match_123", match.Id); // Should not change
        Assert.Equal(MatchStatus.Pendent, match.Status); // Should not change
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeIdAndStatus()
    {
        // Arrange
        var match = new Models.Match
        {
            Id = "match_123",
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            minPlayers = 2,
            maxPlayers = 5,
            Sport = Sports.Football,
            Status = MatchStatus.Confirmed,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateMatchDto
        {
            UserId = "creator_123",
            MatchId = "match_999", // Different ID
            Location = new Location(1, 1, "New City", "New Country"),
            Address = "New Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2),
            Description = "Updated Match",
            MinPlayers = 3,
            MaxPlayers = 6,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Private
        };

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal("match_123", match.Id); // Should remain unchanged
        Assert.Equal(MatchStatus.Confirmed, match.Status); // Should remain unchanged
    }

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        var match = new Models.Match
        {
            Id = "match_123",
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            minPlayers = 2,
            maxPlayers = 5,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateMatchDto
        {
            UserId = "creator_123",
            MatchId = "match_123",
            Location = new Location(1, 1, "New City", "New Country"),
            Address = "Updated Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(2),
            Description = "Updated Match",
            MinPlayers = 3,
            MaxPlayers = 6,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Private
        };

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.NotNull(match.UpdatedAtUtc);
        Assert.True(match.UpdatedAtUtc > match.CreatedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldSpecifyUtcKindForMatchDateTime()
    {
        // Arrange
        var match = new Models.Match
        {
            Id = "match_123",
            Location = new Location(0, 0, "City", "Country"),
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            minPlayers = 2,
            maxPlayers = 5,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateMatchDto
        {
            UserId = "creator_123",
            MatchId = "match_123",
            Location = new Location(1, 1, "New City", "New Country"),
            Address = "Updated Address",
            MatchDateTimeUtc = new DateTime(2025, 12, 31, 10, 0, 0, DateTimeKind.Unspecified),
            Description = "Updated Match",
            MinPlayers = 3,
            MaxPlayers = 6,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Private
        };

        // Act
        match.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(DateTimeKind.Utc, match.MatchDateTimeUtc.Kind);
    }

    #endregion
}

