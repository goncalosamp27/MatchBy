using MatchBy.DTOs.PlayerRating;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.PlayerRating;

public class PlayerRatingMappingsTests
{
    #region ToDto Tests

    [Fact]
    public void ToDto_WithCompletePlayerRating_ShouldMapAllProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        DateTime updatedAt = DateTime.UtcNow.AddDays(1);

        var sender = new ApplicationUser
        {
            Id = "sender_123",
            UserName = "sender",
            DisplayName = "Sender User",
            CreatedAtUtc = createdAt
        };

        var receiver = new ApplicationUser
        {
            Id = "receiver_456",
            UserName = "receiver",
            DisplayName = "Receiver User",
            CreatedAtUtc = createdAt
        };

        var match = new Models.Match
        {
            Id = "match_789",
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
            CreatedAtUtc = createdAt
        };

        var playerRating = new Models.PlayerRating
        {
            Id = "playerrating_999",
            Rating = 4.5f,
            SentById = "sender_123",
            SentBy = sender,
            ReceivedById = "receiver_456",
            ReceivedBy = receiver,
            MatchId = "match_789",
            Match = match,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            DeletedAtUtc = null
        };

        // Act
        PlayerRatingDto dto = playerRating.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("playerrating_999", dto.Id);
        Assert.Equal(4.5f, dto.Rating);
        Assert.Equal("sender_123", dto.SentById);
        Assert.NotNull(dto.SentBy);
        Assert.Equal("sender_123", dto.SentBy.Id);
        Assert.Equal("receiver_456", dto.ReceivedById);
        Assert.NotNull(dto.ReceivedBy);
        Assert.Equal("receiver_456", dto.ReceivedBy.Id);
        Assert.Equal("match_789", dto.MatchId);
        Assert.NotNull(dto.Match);
        Assert.Equal("match_789", dto.Match.Id);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void ToDto_WithNullNavigationProperties_ShouldMapCorrectly()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;

        var playerRating = new Models.PlayerRating
        {
            Id = "playerrating_999",
            Rating = 3.0f,
            SentById = "sender_123",
            SentBy = null,
            ReceivedById = "receiver_456",
            ReceivedBy = null,
            MatchId = "match_789",
            Match = null,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        PlayerRatingDto dto = playerRating.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("playerrating_999", dto.Id);
        Assert.Equal(3.0f, dto.Rating);
        Assert.Null(dto.SentBy);
        Assert.Null(dto.ReceivedBy);
        Assert.Null(dto.Match);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    #endregion

    #region ToEntity Tests

    [Fact]
    public void ToEntity_WithCreatePlayerRatingDto_ShouldMapAllProperties()
    {
        // Arrange
        var createDto = new CreatePlayerRatingDto
        {
            Rating = 4.5f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789"
        };

        // Act
        Models.PlayerRating entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("playerrating_", entity.Id);
        Assert.Equal(4.5f, entity.Rating);
        Assert.Equal("sender_123", entity.SentById);
        Assert.Equal("receiver_456", entity.ReceivedById);
        Assert.Equal("match_789", entity.MatchId);
        Assert.NotEqual(DateTime.MinValue, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void ToEntity_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Arrange
        var createDto = new CreatePlayerRatingDto
        {
            Rating = 4.0f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789"
        };

        // Act
        Models.PlayerRating entity1 = createDto.ToEntity();
        Models.PlayerRating entity2 = createDto.ToEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void ToEntity_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var createDto = new CreatePlayerRatingDto
        {
            Rating = 5.0f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789"
        };

        DateTime beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        Models.PlayerRating entity = createDto.ToEntity();
        DateTime afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(entity.CreatedAtUtc >= beforeCreation && entity.CreatedAtUtc <= afterCreation);
    }

    #endregion

    #region UpdateEntity Tests

    [Fact]
    public void UpdateEntity_WithUpdatePlayerRatingDto_ShouldUpdateRating()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow.AddDays(-10);
        var playerRating = new Models.PlayerRating
        {
            Id = "playerrating_123",
            Rating = 3.0f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789",
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdatePlayerRatingDto
        {
            Id = "playerrating_123",
            Rating = 4.5f,
            SentById = "sender_123"
        };

        // Act
        DateTime beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        playerRating.UpdateEntity(updateDto);
        DateTime afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.Equal(4.5f, playerRating.Rating);
        Assert.NotNull(playerRating.UpdatedAtUtc);
        Assert.True(playerRating.UpdatedAtUtc >= beforeUpdate && playerRating.UpdatedAtUtc <= afterUpdate);
        Assert.Equal(createdAt, playerRating.CreatedAtUtc); // Should not change
        Assert.Equal("playerrating_123", playerRating.Id); // Should not change
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeIdAndOtherProperties()
    {
        // Arrange
        var playerRating = new Models.PlayerRating
        {
            Id = "playerrating_123",
            Rating = 3.0f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdatePlayerRatingDto
        {
            Id = "playerrating_999", // Different ID
            Rating = 5.0f,
            SentById = "sender_456" // Different sender
        };

        // Act
        playerRating.UpdateEntity(updateDto);

        // Assert
        Assert.Equal("playerrating_123", playerRating.Id); // Should remain unchanged
        Assert.Equal("sender_123", playerRating.SentById); // Should remain unchanged
        Assert.Equal("receiver_456", playerRating.ReceivedById); // Should remain unchanged
        Assert.Equal("match_789", playerRating.MatchId); // Should remain unchanged
        Assert.Equal(5.0f, playerRating.Rating); // Should be updated
    }

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        var playerRating = new Models.PlayerRating
        {
            Id = "playerrating_123",
            Rating = 3.0f,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdatePlayerRatingDto
        {
            Id = "playerrating_123",
            Rating = 4.0f,
            SentById = "sender_123"
        };

        // Act
        playerRating.UpdateEntity(updateDto);

        // Assert
        Assert.NotNull(playerRating.UpdatedAtUtc);
        Assert.True(playerRating.UpdatedAtUtc > playerRating.CreatedAtUtc);
    }

    #endregion
}

