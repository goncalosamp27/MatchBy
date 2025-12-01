using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.TeamInvite;

public class TeamInviteMappingsTests
{
    [Fact]
    public void ToDto_WithCompleteTeamInvite_ShouldMapAllProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        DateTime expiresAt = DateTime.UtcNow.AddDays(7);
        
        var teamInvite = new MatchBy.Models.TeamInvite
        {
            Id = "teaminvite_123",
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            AcceptedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        TeamInviteDto dto = teamInvite.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("teaminvite_123", dto.Id);
        Assert.Equal("Join our team!", dto.Content);
        Assert.Equal("sender_123", dto.SenderId);
        Assert.Equal("receiver_456", dto.ReceiverId);
        Assert.Equal("team_789", dto.TeamId);
        Assert.Equal(InviteStatus.Pending, dto.Status);
        Assert.Equal(expiresAt, dto.ExpiresAtUtc);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
    }

    [Fact]
    public void ToEntity_WithCreateTeamInviteDto_ShouldMapAllProperties()
    {
        // Arrange
        DateTime expiresAt = DateTime.UtcNow.AddDays(14);
        var createDto = new CreateTeamInviteDto
        {
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            ExpiresAtUtc = expiresAt
        };

        // Act
        MatchBy.Models.TeamInvite entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("teaminvite_", entity.Id);
        Assert.Equal("Join our team!", entity.Content);
        Assert.Equal("sender_123", entity.SenderId);
        Assert.Equal("receiver_456", entity.ReceiverId);
        Assert.Equal("team_789", entity.TeamId);
        Assert.Equal(InviteStatus.Pending, entity.Status);
        Assert.Equal(expiresAt, entity.ExpiresAtUtc);
    }

    [Fact]
    public void ToEntity_WithNullExpiresAtUtc_ShouldSetDefault7Days()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            ExpiresAtUtc = null
        };
        DateTime beforeCreation = DateTime.UtcNow.AddDays(6);
        DateTime afterCreation = DateTime.UtcNow.AddDays(8);

        // Act
        MatchBy.Models.TeamInvite entity = createDto.ToEntity();

        // Assert
        Assert.True(entity.ExpiresAtUtc >= beforeCreation && entity.ExpiresAtUtc <= afterCreation);
    }
}

