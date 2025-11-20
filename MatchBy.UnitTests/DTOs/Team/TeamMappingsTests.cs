using MatchBy.DTOs.Team;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Team;

public class TeamMappingsTests
{
    #region ToDto Tests

    [Fact]
    public void ToDto_WithCompleteTeam_ShouldMapAllProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        DateTime updatedAt = DateTime.UtcNow.AddDays(1);
        
        var owner = new ApplicationUser
        {
            Id = "owner_123",
            UserName = "teamowner",
            DisplayName = "Team Owner",
            CreatedAtUtc = createdAt
        };

        var member1 = new ApplicationUser
        {
            Id = "member_456",
            UserName = "member1",
            DisplayName = "Member One",
            CreatedAtUtc = createdAt
        };

        var team = new Models.Team
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            Owner = owner,
            Members = new List<ApplicationUser> { owner, member1 },
            ConversationId = "conv_789",
            Conversation = null,
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            Image = new FileStore(
                Url: "https://example.com/team.jpg",
                ExpireDateTimeUtc: DateTime.UtcNow.AddDays(30),
                Key: "teams/team_123",
                FileCategory: FileCategory.TeamImage,
                FileType: FileType.Image,
                CreatedAtUtc: createdAt
            ),
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            DeletedAtUtc = null
        };

        // Act
        TeamDto dto = team.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("team_123", dto.Id);
        Assert.Equal("Team Alpha", dto.Name);
        Assert.Equal("Test Description", dto.Description);
        Assert.Equal("owner_123", dto.OwnerId);
        Assert.NotNull(dto.Owner);
        Assert.Equal("owner_123", dto.Owner.Id);
        Assert.Equal(2, dto.Members.Count);
        Assert.Equal("conv_789", dto.ConversationId);
        Assert.Equal(10, dto.MaxMembers);
        Assert.Equal(TeamPrivacy.Public, dto.Privacy);
        Assert.Equal("https://example.com/team.jpg", dto.ImageUrl);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void ToDto_WithNullOptionalProperties_ShouldMapCorrectly()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        
        var team = new Models.Team
        {
            Id = "team_123",
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            Owner = null,
            Members = new List<ApplicationUser>(),
            ConversationId = null,
            Conversation = null,
            MaxMembers = 10,
            Privacy = TeamPrivacy.Private,
            Image = null,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        TeamDto dto = team.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("team_123", dto.Id);
        Assert.Null(dto.Owner);
        Assert.Empty(dto.Members);
        Assert.Null(dto.ConversationId);
        Assert.Null(dto.ImageUrl);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    #endregion

    #region ToEntity Tests

    [Fact]
    public void ToEntity_WithCreateTeamDto_ShouldMapAllProperties()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            MaxMembers = 15,
            Privacy = TeamPrivacy.Private,
            MembersIds = new List<string> { "owner_123", "user_456" },
            File = null
        };

        // Act
        Models.Team entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("team_", entity.Id);
        Assert.Equal("Team Alpha", entity.Name);
        Assert.Equal("Test Description", entity.Description);
        Assert.Equal("owner_123", entity.OwnerId);
        Assert.Equal(15, entity.MaxMembers);
        Assert.Equal(TeamPrivacy.Private, entity.Privacy);
        Assert.NotNull(entity.Members);
        Assert.Empty(entity.Members);
        Assert.NotEqual(DateTime.MinValue, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void ToEntity_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123" },
            File = null
        };

        // Act
        Models.Team entity1 = createDto.ToEntity();
        Models.Team entity2 = createDto.ToEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void ToEntity_ShouldInitializeEmptyMembersCollection()
    {
        // Arrange
        var createDto = new CreateTeamDto
        {
            Name = "Team Alpha",
            Description = "Test Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123", "user_456", "user_789" },
            File = null
        };

        // Act
        Models.Team entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity.Members);
        Assert.Empty(entity.Members);
    }

    #endregion

    #region UpdateEntity Tests

    [Fact]
    public void UpdateEntity_WithUpdateTeamDto_ShouldUpdateProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow.AddDays(-10);
        var team = new Models.Team
        {
            Id = "team_123",
            Name = "Old Name",
            Description = "Old Description",
            OwnerId = "owner_123",
            MaxMembers = 5,
            Privacy = TeamPrivacy.Private,
            Members = new List<ApplicationUser>(),
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team_123",
            Name = "New Name",
            Description = "New Description",
            OwnerId = "owner_123",
            MaxMembers = 20,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123", "user_456" },
            File = null
        };

        // Act
        DateTime beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        team.UpdateEntity(updateDto);
        DateTime afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.Equal("New Name", team.Name);
        Assert.Equal("New Description", team.Description);
        Assert.Equal(20, team.MaxMembers);
        Assert.Equal(TeamPrivacy.Public, team.Privacy);
        Assert.NotNull(team.UpdatedAtUtc);
        Assert.True(team.UpdatedAtUtc >= beforeUpdate && team.UpdatedAtUtc <= afterUpdate);
        Assert.Equal(createdAt, team.CreatedAtUtc); // Should not change
        Assert.Equal("team_123", team.Id); // Should not change
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeId()
    {
        // Arrange
        var team = new Models.Team
        {
            Id = "team_123",
            Name = "Original Name",
            Description = "Original Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            Members = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team_999", // Different ID
            Name = "New Name",
            Description = "New Description",
            OwnerId = "owner_123",
            MaxMembers = 15,
            Privacy = TeamPrivacy.Private,
            MembersIds = new List<string> { "owner_123" },
            File = null
        };

        // Act
        team.UpdateEntity(updateDto);

        // Assert
        Assert.Equal("team_123", team.Id); // Should remain unchanged
    }

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        var team = new Models.Team
        {
            Id = "team_123",
            Name = "Original Name",
            Description = "Original Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            Members = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var updateDto = new UpdateTeamDto
        {
            Id = "team_123",
            Name = "Updated Name",
            Description = "Updated Description",
            OwnerId = "owner_123",
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            MembersIds = new List<string> { "owner_123" },
            File = null
        };

        // Act
        team.UpdateEntity(updateDto);

        // Assert
        Assert.NotNull(team.UpdatedAtUtc);
        Assert.True(team.UpdatedAtUtc > team.CreatedAtUtc);
    }

    #endregion
}

