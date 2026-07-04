using MatchBy.DTOs.Chat.Messages;
using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.Mappings;

public class ChatMessageMappingsTests
{
    [Fact]
    public void ToDto_WhenChatMessageIsValid_ShouldMapToDto()
    {
        // Arrange
        ApplicationUser sender = CreateValidUser();
        ChatMessage chatMessage = CreateValidChatMessage(sender);

        // Act
        ChatMessageDto dto = chatMessage.ToDto();

        // Assert
        Assert.Equal(chatMessage.Id, dto.Id);
        Assert.Equal(chatMessage.Content, dto.Content);
        Assert.Equal(chatMessage.SenderId, dto.SenderId);
        Assert.NotNull(dto.Sender);
        Assert.Equal(sender.Id, dto.Sender.Id);
        Assert.Equal(chatMessage.Location, dto.Location);
        Assert.Equal(chatMessage.InviteUrl, dto.InviteUrl);
        Assert.Equal(chatMessage.ReplyToMessageId, dto.ReplyToMessageId);
        Assert.Equal(chatMessage.ConversationId, dto.ConversationId);
        Assert.Equal(chatMessage.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(chatMessage.UpdatedAtUtc, dto.UpdatedAtUtc);
    }

    [Fact]
    public void ToDto_WhenSenderIsNull_ShouldThrowInvalidOperationException()
    {
        // Arrange
        ChatMessage chatMessage = CreateValidChatMessage(null!);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => chatMessage.ToDto());
        Assert.Equal("Cannot map ChatMessage to ChatMessageDto when Sender is null.", exception.Message);
    }

    [Fact]
    public void ToDto_WhenReplyToMessageIsNotNull_ShouldMapReplyToMessage()
    {
        // Arrange
        ApplicationUser sender = CreateValidUser();
        ChatMessage replyToMessage = CreateValidChatMessage(sender);
        ChatMessage chatMessage = CreateValidChatMessage(sender);
        chatMessage.ReplyToMessageId = replyToMessage.Id;
        chatMessage.ReplyToMessage = replyToMessage;

        // Act
        ChatMessageDto dto = chatMessage.ToDto();

        // Assert
        Assert.NotNull(dto.ReplyToMessage);
        Assert.Equal(replyToMessage.Id, dto.ReplyToMessage.Id);
        Assert.Equal(replyToMessage.Content, dto.ReplyToMessage.Content);
    }

    [Fact]
    public void ToDto_WhenReplyToMessageIsNull_ShouldReturnNullReplyToMessage()
    {
        // Arrange
        ApplicationUser sender = CreateValidUser();
        ChatMessage chatMessage = CreateValidChatMessage(sender);
        chatMessage.ReplyToMessageId = null;
        chatMessage.ReplyToMessage = null;

        // Act
        ChatMessageDto dto = chatMessage.ToDto();

        // Assert
        Assert.Null(dto.ReplyToMessage);
        Assert.Null(dto.ReplyToMessageId);
    }

    [Fact]
    public void ToDto_WhenLocationIsNotNull_ShouldMapLocation()
    {
        // Arrange
        ApplicationUser sender = CreateValidUser();
        ChatMessage chatMessage = CreateValidChatMessage(sender);
        var location = new Location(40.7128, -74.0060, "New York", "USA");
        chatMessage.Location = location;

        // Act
        ChatMessageDto dto = chatMessage.ToDto();

        // Assert
        Assert.NotNull(dto.Location);
        Assert.Equal(location.Latitude, dto.Location.Latitude);
        Assert.Equal(location.Longitude, dto.Location.Longitude);
        Assert.Equal(location.City, dto.Location.City);
        Assert.Equal(location.Country, dto.Location.Country);
    }

    [Fact]
    public void ToDto_WhenLocationIsNull_ShouldReturnNullLocation()
    {
        // Arrange
        ApplicationUser sender = CreateValidUser();
        ChatMessage chatMessage = CreateValidChatMessage(sender);
        chatMessage.Location = null;

        // Act
        ChatMessageDto dto = chatMessage.ToDto();

        // Assert
        Assert.Null(dto.Location);
    }

    [Fact]
    public void ToEntity_WhenCreateChatMessageDtoIsValid_ShouldMapToEntity()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto();

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.StartsWith("chatMessage_", entity.Id);
        Assert.Equal(dto.Content, entity.Content);
        Assert.Equal(dto.CreatorUserId, entity.SenderId);
        Assert.Equal(dto.ReplyToMessageId, entity.ReplyToMessageId);
        Assert.Equal(dto.ConversationId, entity.ConversationId);
        Assert.Equal(dto.Location, entity.Location);
        Assert.Equal(dto.InviteUrl, entity.InviteUrl);
        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
    }

    [Fact]
    public void ToEntity_WhenContentIsNull_ShouldMapNullContent()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { Content = null };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.Content);
    }

    [Fact]
    public void ToEntity_WhenLocationIsNotNull_ShouldMapLocation()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto();
        var location = new Location(40.7128, -74.0060, "New York", "USA");
        dto = dto with { Location = location };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.NotNull(entity.Location);
        Assert.Equal(location.Latitude, entity.Location.Latitude);
        Assert.Equal(location.Longitude, entity.Location.Longitude);
        Assert.Equal(location.City, entity.Location.City);
        Assert.Equal(location.Country, entity.Location.Country);
    }

    [Fact]
    public void ToEntity_WhenLocationIsNull_ShouldMapNullLocation()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { Location = null };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.Location);
    }

    [Fact]
    public void ToEntity_WhenReplyToMessageIdIsNotNull_ShouldMapReplyToMessageId()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { ReplyToMessageId = "reply-to-id" };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Equal("reply-to-id", entity.ReplyToMessageId);
    }

    [Fact]
    public void ToEntity_WhenReplyToMessageIdIsNull_ShouldMapNullReplyToMessageId()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { ReplyToMessageId = null };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.ReplyToMessageId);
    }

    [Fact]
    public void ToEntity_WhenInviteUrlIsNotNull_ShouldMapInviteUrl()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { InviteUrl = "https://example.com/invite" };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Equal("https://example.com/invite", entity.InviteUrl);
    }

    [Fact]
    public void ToEntity_WhenInviteUrlIsNull_ShouldMapNullInviteUrl()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidCreateChatMessageDto() with { InviteUrl = null };

        // Act
        ChatMessage entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.InviteUrl);
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

    private static ChatMessage CreateValidChatMessage(ApplicationUser sender)
    {
        return new ChatMessage
        {
            Id = "chat-message-id",
            Content = "Test message",
            SenderId = sender?.Id ?? "sender-id",
            Sender = sender,
            ConversationId = "conversation-id",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }

    private static CreateChatMessageDto CreateValidCreateChatMessageDto()
    {
        return new CreateChatMessageDto
        {
            Content = "Test message",
            CreatorUserId = "creator-user-id",
            ConversationId = "conversation-id",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
    }
}



