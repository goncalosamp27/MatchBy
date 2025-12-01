using MatchBy.DTOs.MatchInvite;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.MatchInvite;

public class MatchInviteMappingsTests
{
    [Fact]
    public void ToDto_WithCompleteMatchInvite_ShouldMapAllProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        DateTime expiresAt = DateTime.UtcNow.AddDays(7);
        
        var matchInvite = new MatchBy.Models.MatchInvite
        {
            Id = "matchinvite_123",
            Content = "Join our match!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            MatchId = "match_789",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = expiresAt,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            AcceptedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        MatchInviteDto dto = matchInvite.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("matchinvite_123", dto.Id);
        Assert.Equal("Join our match!", dto.Content);
        Assert.Equal("sender_123", dto.SenderId);
        Assert.Equal("receiver_456", dto.ReceiverId);
        Assert.Equal("match_789", dto.MatchId);
        Assert.Equal(InviteStatus.Pending, dto.Status);
        Assert.Equal(expiresAt, dto.ExpiresAtUtc);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
    }

    [Fact]
    public void ToEntity_WithCreateMatchInviteDto_ShouldMapAllProperties()
    {
        // Arrange
        DateTime expiresAt = DateTime.UtcNow.AddDays(14);
        var createDto = new CreateMatchInviteDto
        {
            Content = "Join our match!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            MatchId = "match_789",
            ExpiresAtUtc = expiresAt
        };

        // Act
        MatchBy.Models.MatchInvite entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("matchinvite_", entity.Id);
        Assert.Equal("Join our match!", entity.Content);
        Assert.Equal("sender_123", entity.SenderId);
        Assert.Equal("receiver_456", entity.ReceiverId);
        Assert.Equal("match_789", entity.MatchId);
        Assert.Equal(InviteStatus.Pending, entity.Status);
        Assert.Equal(expiresAt, entity.ExpiresAtUtc);
    }

    [Fact]
    public void ToEntity_WithNullExpiresAtUtc_ShouldSetDefault7Days()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            Content = "Join our match!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            MatchId = "match_789",
            ExpiresAtUtc = null
        };
        DateTime beforeCreation = DateTime.UtcNow.AddDays(6);
        DateTime afterCreation = DateTime.UtcNow.AddDays(8);

        // Act
        MatchBy.Models.MatchInvite entity = createDto.ToEntity();

        // Assert
        Assert.True(entity.ExpiresAtUtc >= beforeCreation && entity.ExpiresAtUtc <= afterCreation);
    }

}

