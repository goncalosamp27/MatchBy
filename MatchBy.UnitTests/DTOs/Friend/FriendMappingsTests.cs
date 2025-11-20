using MatchBy.DTOs.Friend;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Friend;

public class FriendMappingsTests
{
    #region ToDto Tests

    [Fact]
    public void ToDto_WithCompleteFriend_ShouldMapAllProperties()
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

        var friend = new Models.Friend
        {
            Id = "friend_789",
            SenderId = "sender_123",
            Sender = sender,
            ReceiverId = "receiver_456",
            Receiver = receiver,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt,
            DeletedAtUtc = null
        };

        // Act
        FriendDto dto = friend.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("friend_789", dto.Id);
        Assert.Equal("sender_123", dto.SenderId);
        Assert.NotNull(dto.Sender);
        Assert.Equal("sender_123", dto.Sender.Id);
        Assert.Equal("receiver_456", dto.ReceiverId);
        Assert.NotNull(dto.Receiver);
        Assert.Equal("receiver_456", dto.Receiver.Id);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Equal(updatedAt, dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void ToDto_WithNullNavigationProperties_ShouldMapCorrectly()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;

        var friend = new Models.Friend
        {
            Id = "friend_789",
            SenderId = "sender_123",
            Sender = null,
            ReceiverId = "receiver_456",
            Receiver = null,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        FriendDto dto = friend.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("friend_789", dto.Id);
        Assert.Equal("sender_123", dto.SenderId);
        Assert.Null(dto.Sender);
        Assert.Equal("receiver_456", dto.ReceiverId);
        Assert.Null(dto.Receiver);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    #endregion

    #region ToEntity Tests

    [Fact]
    public void ToEntity_WithCreateFriendDto_ShouldMapAllProperties()
    {
        // Arrange
        var createDto = new CreateFriendDto
        {
            SenderId = "sender_123",
            ReceiverId = "receiver_456"
        };

        // Act
        Models.Friend entity = createDto.ToEntity();

        // Assert
        Assert.NotNull(entity);
        Assert.StartsWith("friend_", entity.Id);
        Assert.Equal("sender_123", entity.SenderId);
        Assert.Equal("receiver_456", entity.ReceiverId);
        Assert.NotEqual(DateTime.MinValue, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
        Assert.Null(entity.DeletedAtUtc);
    }

    [Fact]
    public void ToEntity_MultipleCalls_ShouldGenerateUniqueIds()
    {
        // Arrange
        var createDto = new CreateFriendDto
        {
            SenderId = "sender_123",
            ReceiverId = "receiver_456"
        };

        // Act
        Models.Friend entity1 = createDto.ToEntity();
        Models.Friend entity2 = createDto.ToEntity();

        // Assert
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void ToEntity_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var createDto = new CreateFriendDto
        {
            SenderId = "sender_123",
            ReceiverId = "receiver_456"
        };

        DateTime beforeCreation = DateTime.UtcNow.AddSeconds(-1);

        // Act
        Models.Friend entity = createDto.ToEntity();
        DateTime afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(entity.CreatedAtUtc >= beforeCreation && entity.CreatedAtUtc <= afterCreation);
    }

    #endregion

    #region UpdateEntity Tests

    [Fact]
    public void UpdateEntity_ShouldSetUpdatedAtUtc()
    {
        // Arrange
        var friend = new Models.Friend
        {
            Id = "friend_789",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5),
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        DateTime beforeUpdate = DateTime.UtcNow.AddSeconds(-1);

        // Act
        friend.UpdateEntity();
        DateTime afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(friend.UpdatedAtUtc);
        Assert.True(friend.UpdatedAtUtc >= beforeUpdate && friend.UpdatedAtUtc <= afterUpdate);
        Assert.True(friend.UpdatedAtUtc > friend.CreatedAtUtc);
    }

    [Fact]
    public void UpdateEntity_ShouldNotChangeOtherProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow.AddDays(-10);
        var friend = new Models.Friend
        {
            Id = "friend_789",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Act
        friend.UpdateEntity();

        // Assert
        Assert.Equal("friend_789", friend.Id);
        Assert.Equal("sender_123", friend.SenderId);
        Assert.Equal("receiver_456", friend.ReceiverId);
        Assert.Equal(createdAt, friend.CreatedAtUtc);
        Assert.Null(friend.DeletedAtUtc);
    }

    #endregion
}

