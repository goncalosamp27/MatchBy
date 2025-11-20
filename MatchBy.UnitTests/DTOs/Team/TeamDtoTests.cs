using MatchBy.DTOs.Team;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Team;

public class TeamDtoTests
{
    [Fact]
    public void TeamDto_ShouldInitializeWithAllRequiredProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        var members = new List<UserDto>
        {
            new(id: "user_1", displayName: "User 1", avatarUrl: null)
        };

        // Act
        var dto = new TeamDto
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Test team description",
            OwnerId = "user_1",
            Owner = null,
            Members = members,
            ConversationId = null,
            Conversation = null,
            MaxMembers = 10,
            ImageUrl = null,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.Equal("team_123", dto.Id);
        Assert.Equal("Team Alpha", dto.Name);
        Assert.Equal("Test team description", dto.Description);
        Assert.Equal("user_1", dto.OwnerId);
        Assert.Equal(10, dto.MaxMembers);
        Assert.Equal(TeamPrivacy.Public, dto.Privacy);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Single(dto.Members);
    }

    [Fact]
    public void TeamDto_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var dto = new TeamDto
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Test team description",
            OwnerId = "user_1",
            Owner = null,
            Members = new List<UserDto>(),
            ConversationId = null,
            Conversation = null,
            MaxMembers = 10,
            ImageUrl = null,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.Null(dto.Owner);
        Assert.Null(dto.ConversationId);
        Assert.Null(dto.Conversation);
        Assert.Null(dto.ImageUrl);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void TeamDto_ShouldBeRecord_WithValueEquality()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        var members = new List<UserDto>();

        var dto1 = new TeamDto
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Description",
            OwnerId = "user_1",
            Owner = null,
            Members = members,
            ConversationId = null,
            Conversation = null,
            MaxMembers = 10,
            ImageUrl = null,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var dto2 = new TeamDto
        {
            Id = "team_456",
            Name = "Team Beta",
            Description = "Different description",
            OwnerId = "user_2",
            Owner = null,
            Members = members,
            ConversationId = null,
            Conversation = null,
            MaxMembers = 15,
            ImageUrl = null,
            Privacy = TeamPrivacy.Private,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.NotEqual(dto1, dto2);
    }

    [Fact]
    public void TeamDto_ShouldSupportWithExpression()
    {
        // Arrange
        var originalDto = new TeamDto
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Description",
            OwnerId = "user_1",
            Owner = null,
            Members = new List<UserDto>(),
            ConversationId = null,
            Conversation = null,
            MaxMembers = 10,
            ImageUrl = null,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        TeamDto modifiedDto = originalDto with { Name = "Team Beta", MaxMembers = 15 };

        // Assert
        Assert.Equal("Team Beta", modifiedDto.Name);
        Assert.Equal(15, modifiedDto.MaxMembers);
        Assert.Equal("team_123", modifiedDto.Id);
        Assert.NotEqual(originalDto, modifiedDto);
    }
}

