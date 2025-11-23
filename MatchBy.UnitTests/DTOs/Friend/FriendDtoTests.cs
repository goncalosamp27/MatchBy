using MatchBy.DTOs.Friend;
using MatchBy.DTOs.User;

namespace MatchBy.UnitTests.DTOs.Friend;

public class FriendDtoTests
{
    [Fact]
    public void FriendDto_ShouldInitializeWithAllRequiredProperties()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;
        var sender = new UserDto { Id = "user_1", DisplayName = "User 1", AvatarUrl = null };
        var receiver = new UserDto { Id = "user_2", DisplayName = "User 2", AvatarUrl = null };

        // Act
        var dto = new FriendDto
        {
            Id = "friend_123",
            SenderId = "user_1",
            Sender = sender,
            ReceiverId = "user_2",
            Receiver = receiver,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.Equal("friend_123", dto.Id);
        Assert.Equal("user_1", dto.SenderId);
        Assert.Equal("user_2", dto.ReceiverId);
        Assert.NotNull(dto.Sender);
        Assert.NotNull(dto.Receiver);
        Assert.Equal(createdAt, dto.CreatedAtUtc);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void FriendDto_OptionalProperties_CanBeNull()
    {
        // Arrange & Act
        var dto = new FriendDto
        {
            Id = "friend_123",
            SenderId = "user_1",
            Sender = null,
            ReceiverId = "user_2",
            Receiver = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.Null(dto.Sender);
        Assert.Null(dto.Receiver);
        Assert.Null(dto.UpdatedAtUtc);
        Assert.Null(dto.DeletedAtUtc);
    }

    [Fact]
    public void FriendDto_ShouldBeRecord_WithValueEquality()
    {
        // Arrange
        DateTime createdAt = DateTime.UtcNow;

        var dto1 = new FriendDto
        {
            Id = "friend_123",
            SenderId = "user_1",
            Sender = null,
            ReceiverId = "user_2",
            Receiver = null,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        var dto2 = new FriendDto
        {
            Id = "friend_456",
            SenderId = "user_3",
            Sender = null,
            ReceiverId = "user_4",
            Receiver = null,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        // Assert
        Assert.NotEqual(dto1, dto2);
    }

    [Fact]
    public void FriendDto_ShouldSupportWithExpression()
    {
        // Arrange
        var originalDto = new FriendDto
        {
            Id = "friend_123",
            SenderId = "user_1",
            Sender = null,
            ReceiverId = "user_2",
            Receiver = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };

        DateTime updatedAt = DateTime.UtcNow;

        // Act
        FriendDto modifiedDto = originalDto with { UpdatedAtUtc = updatedAt };

        // Assert
        Assert.Equal(updatedAt, modifiedDto.UpdatedAtUtc);
        Assert.Equal("friend_123", modifiedDto.Id);
        Assert.NotEqual(originalDto, modifiedDto);
    }
}

