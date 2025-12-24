using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.Mappings;

public class ConversationMappingsTests
{
    [Fact]
    public void ToDto_WhenConversationIsValid_ShouldMapToDto()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Equal(conversation.Id, dto.Id);
        Assert.Equal(conversation.Type, dto.Type);
        Assert.Equal(conversation.Title, dto.Title);
        Assert.Equal(conversation.Image?.Url, dto.ImageUrl);
        Assert.Equal(conversation.CreatorId, dto.CreatorId);
        Assert.Equal(conversation.TeamId, dto.TeamId);
        Assert.Equal(conversation.MatchId, dto.MatchId);
        Assert.Equal(conversation.CreatedAtUtc, dto.CreatedAtUtc);
        Assert.Equal(conversation.UpdatedAtUtc, dto.UpdatedAtUtc);
        Assert.Equal(conversation.LastMessageContent, dto.LastMessageContent);
        Assert.Equal(conversation.Messages.Count, dto.MessagesCount);
        Assert.Equal(conversation.LastMessageAtUtc, dto.LastMessageAtUtc);
        Assert.NotNull(dto.Participants);
    }

    [Fact]
    public void ToDto_WhenImageIsNull_ShouldMapNullImageUrl()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        conversation.Image = null;

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Null(dto.ImageUrl);
    }

    [Fact]
    public void ToDto_WhenImageIsNotNull_ShouldMapImageUrl()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        var image = new FileStore("https://example.com/image.jpg", DateTime.UtcNow.AddDays(1), "image-key", FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        conversation.Image = image;

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Equal("https://example.com/image.jpg", dto.ImageUrl);
    }

    [Fact]
    public void ToDto_WhenParticipantsIsEmpty_ShouldMapEmptyParticipantsList()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        conversation.Participants = new List<ApplicationUser>();

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.NotNull(dto.Participants);
        Assert.Empty(dto.Participants);
    }

    [Fact]
    public void ToDto_WhenParticipantsHasMultipleUsers_ShouldMapAllParticipants()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        ApplicationUser user1 = CreateValidUser("user1", "User One", "user1");
        ApplicationUser user2 = CreateValidUser("user2", "User Two", "user2");
        conversation.Participants = new List<ApplicationUser> { user1, user2 };

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.NotNull(dto.Participants);
        Assert.Equal(2, dto.Participants.Count);
        Assert.Contains(dto.Participants, p => p.Id == "user1");
        Assert.Contains(dto.Participants, p => p.Id == "user2");
    }

    [Fact]
    public void ToDto_WhenParticipantHasProfileImage_ShouldMapProfileImageUrl()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        ApplicationUser user = CreateValidUser("user1", "User One", "user1");
        var profileImage = new FileStore("https://example.com/profile.jpg", DateTime.UtcNow.AddDays(1), "profile-key", FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = profileImage;
        conversation.Participants = new List<ApplicationUser> { user };

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Single(dto.Participants);
        Assert.Equal("https://example.com/profile.jpg", dto.Participants[0].ImageUrl);
    }

    [Fact]
    public void ToDto_WhenParticipantHasNullProfileImage_ShouldMapNullImageUrl()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        ApplicationUser user = CreateValidUser("user1", "User One", "user1");
        user.ProfileImage = null;
        conversation.Participants = new List<ApplicationUser> { user };

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Single(dto.Participants);
        Assert.Null(dto.Participants[0].ImageUrl);
    }

    [Fact]
    public void ToDto_WhenMessagesIsEmpty_ShouldMapMessagesCountAsZero()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        conversation.Messages = new List<ChatMessage>();

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Equal(0, dto.MessagesCount);
    }

    [Fact]
    public void ToDto_WhenMessagesHasMultipleMessages_ShouldMapCorrectMessagesCount()
    {
        // Arrange
        Conversation conversation = CreateValidConversation();
        ChatMessage message1 = CreateValidChatMessage();
        ChatMessage message2 = CreateValidChatMessage();
        conversation.Messages = new List<ChatMessage> { message1, message2 };

        // Act
        ConversationDto dto = conversation.ToDto();

        // Assert
        Assert.Equal(2, dto.MessagesCount);
    }

    [Fact]
    public void ToEntity_WhenCreateConversationDtoIsValid_ShouldMapToEntity()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto();

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.StartsWith("conversation_", entity.Id);
        Assert.Equal(dto.ConversationType, entity.Type);
        Assert.Equal(dto.Title, entity.Title);
        Assert.Equal(dto.CreatorUserId, entity.CreatorId);
        Assert.Equal(dto.TeamId, entity.TeamId);
        Assert.Equal(dto.MatchId, entity.MatchId);
        Assert.Null(entity.Image);
        Assert.NotNull(entity.Participants);
        Assert.Empty(entity.Participants);
        Assert.NotNull(entity.Messages);
        Assert.Empty(entity.Messages);
        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.Null(entity.UpdatedAtUtc);
        Assert.Null(entity.LastMessageAtUtc);
    }

    [Fact]
    public void ToEntity_WhenTitleIsNull_ShouldMapNullTitle()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { Title = null };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.Title);
    }

    [Fact]
    public void ToEntity_WhenTitleIsNotNull_ShouldMapTitle()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { Title = "Test Conversation" };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal("Test Conversation", entity.Title);
    }

    [Fact]
    public void ToEntity_WhenTeamIdIsNull_ShouldMapNullTeamId()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { TeamId = null };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.TeamId);
    }

    [Fact]
    public void ToEntity_WhenTeamIdIsNotNull_ShouldMapTeamId()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { TeamId = "team-id" };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal("team-id", entity.TeamId);
    }

    [Fact]
    public void ToEntity_WhenMatchIdIsNull_ShouldMapNullMatchId()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { MatchId = null };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Null(entity.MatchId);
    }

    [Fact]
    public void ToEntity_WhenMatchIdIsNotNull_ShouldMapMatchId()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { MatchId = "match-id" };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal("match-id", entity.MatchId);
    }

    [Fact]
    public void ToEntity_WhenConversationTypeIsPrivate_ShouldMapPrivateType()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { ConversationType = ConversationType.Private };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal(ConversationType.Private, entity.Type);
    }

    [Fact]
    public void ToEntity_WhenConversationTypeIsTeam_ShouldMapTeamType()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { ConversationType = ConversationType.Team };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal(ConversationType.Team, entity.Type);
    }

    [Fact]
    public void ToEntity_WhenConversationTypeIsMatch_ShouldMapMatchType()
    {
        // Arrange
        CreateConversationDto dto = CreateValidCreateConversationDto() with { ConversationType = ConversationType.Match };

        // Act
        Conversation entity = dto.ToEntity();

        // Assert
        Assert.Equal(ConversationType.Match, entity.Type);
    }

    private static Conversation CreateValidConversation()
    {
        return new Conversation
        {
            Id = "conversation-id",
            Type = ConversationType.Private,
            Title = "Test Conversation",
            CreatorId = "creator-id",
            TeamId = null,
            MatchId = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            LastMessageAtUtc = null,
            LastMessageContent = null,
            Participants = new List<ApplicationUser>(),
            Messages = new List<ChatMessage>()
        };
    }

    private static ApplicationUser CreateValidUser(string id, string displayName, string userName)
    {
        return new ApplicationUser
        {
            Id = id,
            DisplayName = displayName,
            UserName = userName
        };
    }

    private static ChatMessage CreateValidChatMessage()
    {
        return new ChatMessage
        {
            Id = "message-id",
            Content = "Test message",
            SenderId = "sender-id",
            ConversationId = "conversation-id",
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static CreateConversationDto CreateValidCreateConversationDto()
    {
        return new CreateConversationDto
        {
            CreatorUserId = "creator-user-id",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "participant-id" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
    }
}



