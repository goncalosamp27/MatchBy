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
        
        var teamInvite = new Models.TeamInvite
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
            DeclinedAtUtc = null,
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
        Models.TeamInvite entity = createDto.ToEntity();

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
        Models.TeamInvite entity = createDto.ToEntity();

        // Assert
        Assert.True(entity.ExpiresAtUtc >= beforeCreation && entity.ExpiresAtUtc <= afterCreation);
    }

    [Fact]
    public void UpdateEntity_ShouldUpdateContentWhenProvided()
    {
        // Arrange
        var teamInvite = new Models.TeamInvite
        {
            Id = "teaminvite_123",
            Content = "Old content",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var updateDto = new UpdateTeamInviteDto
        {
            Id = "teaminvite_123",
            Content = "New content",
            Status = null,
            ExpiresAtUtc = null
        };

        // Act
        teamInvite.UpdateEntity(updateDto);

        // Assert
        Assert.Equal("New content", teamInvite.Content);
    }

    [Fact]
    public void UpdateEntity_ShouldSetAcceptedAtUtcWhenStatusIsAccepted()
    {
        // Arrange
        var teamInvite = new Models.TeamInvite
        {
            Id = "teaminvite_123",
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            AcceptedAtUtc = null
        };

        var updateDto = new UpdateTeamInviteDto
        {
            Id = "teaminvite_123",
            Content = null,
            Status = InviteStatus.Accepted,
            ExpiresAtUtc = null
        };

        // Act
        teamInvite.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(InviteStatus.Accepted, teamInvite.Status);
        Assert.NotNull(teamInvite.AcceptedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldSetDeclinedAtUtcWhenStatusIsDeclined()
    {
        // Arrange
        var teamInvite = new Models.TeamInvite
        {
            Id = "teaminvite_123",
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            DeclinedAtUtc = null
        };

        var updateDto = new UpdateTeamInviteDto
        {
            Id = "teaminvite_123",
            Content = null,
            Status = InviteStatus.Declined,
            ExpiresAtUtc = null
        };

        // Act
        teamInvite.UpdateEntity(updateDto);

        // Assert
        Assert.Equal(InviteStatus.Declined, teamInvite.Status);
        Assert.NotNull(teamInvite.DeclinedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        var teamInvite = new Models.TeamInvite
        {
            Id = "teaminvite_123",
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = null
        };

        var updateDto = new UpdateTeamInviteDto
        {
            Id = "teaminvite_123",
            Content = "Updated content",
            Status = null,
            ExpiresAtUtc = null
        };

        // Act
        teamInvite.UpdateEntity(updateDto);

        // Assert
        Assert.NotNull(teamInvite.UpdatedAtUtc);
        Assert.True(teamInvite.UpdatedAtUtc > teamInvite.CreatedAtUtc);
    }
}

