using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.ChatMessage;
using MatchBy.Repositories.User;
using MatchBy.Services.ChatMessages;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.ChatMessages;

public class ChatMessageServiceTests : IDisposable
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly Mock<IValidator<CreateChatMessageDto>> _createChatMessageValidatorMock;
    private readonly Mock<IValidator<UpdateChatMessageDto>> _updateChatMessageValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ChatMessageService _chatMessageService;

    public ChatMessageServiceTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _createChatMessageValidatorMock = new Mock<IValidator<CreateChatMessageDto>>();
        _updateChatMessageValidatorMock = new Mock<IValidator<UpdateChatMessageDto>>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validators to return valid by default
        _createChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _updateChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _chatMessageService = new ChatMessageService(
            _conversationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _chatMessageRepositoryMock.Object,
            _dbContextFactoryMock.Object,
            _createChatMessageValidatorMock.Object,
            _updateChatMessageValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetChatMessagesAsync Tests

    [Fact]
    public async Task GetChatMessagesAsync_WithValidParameters_ShouldReturnChatMessages()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";
        int pageSize = 10;
        string? cursor = (string?)null;

        var sender = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = "msg1",
                Content = "Test message",
                CreatedAtUtc = DateTime.UtcNow,
                SenderId = userId,
                Sender = sender
            }
        };

        var paginationResponse = new CursorPaginationResponse<List<ChatMessage>>
        {
            Data = messages,
            NextCursor = null
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            Title = "Test Conversation",
            Type = ConversationType.Match,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>
            {
                new() { Id = userId, UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true }
            }
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.GetChatMessagesAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), pageSize, cursor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync(conversationId, userId, pageSize, cursor);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("msg1", result.Data.Data[0].Id);

        _conversationRepositoryMock.Verify(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _chatMessageRepositoryMock.Verify(r => r.GetChatMessagesAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), pageSize, cursor, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "nonexistent";
        string userId = "user1";
        int pageSize = 10;
        string? cursor = (string?)null;

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync(conversationId, userId, pageSize, cursor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Conversation not found or user is not a participant", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithUserNotParticipant_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";
        int pageSize = 10;
        string? cursor = (string?)null;

        var conversation = new Conversation
        {
            Id = conversationId,
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
            }
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync(conversationId, userId, pageSize, cursor);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is not a participant in the conversation", result.ErrorMessages[0]);
    }

    #endregion

    #region GetChatMessageByIdAsync Tests

    [Fact]
    public async Task GetChatMessageByIdAsync_WithValidMessage_ShouldReturnChatMessageDto()
    {
        // Arrange
        string chatMessageId = "msg1";
        string userId = "user1";

        var sender = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        var chatMessage = new ChatMessage
        {
            Id = chatMessageId,
            Content = "Test message",
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "conv1",
            SenderId = userId,
            Sender = sender
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>
            {
                new() { Id = userId, UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true }
            }
        };

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(chatMessageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMessage);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync(chatMessageId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(chatMessageId, result.Data.Id);
    }

    [Fact]
    public async Task GetChatMessageByIdAsync_WithNonExistentMessage_ShouldReturnFailure()
    {
        // Arrange
        string chatMessageId = "nonexistent";
        string userId = "user1";

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(chatMessageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync(chatMessageId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Chat message not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessageByIdAsync_WithUserNotParticipant_ShouldReturnFailure()
    {
        // Arrange
        string chatMessageId = "msg1";
        string userId = "user1";

        var chatMessage = new ChatMessage
        {
            Id = chatMessageId,
            Content = "Test message",
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "conv1",
            SenderId = "user2"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true }
            }
        };

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(chatMessageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatMessage);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync(chatMessageId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is not a participant in the conversation", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateChatMessageAsync Tests

    [Fact]
    public async Task CreateChatMessageAsync_WithValidDto_ShouldCreateChatMessage()
    {
        // Arrange
        string creatorId = "creator1";
        string conversationId = "conv1";

        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = creatorId,
            ConversationId = conversationId,
            Content = "New message content"
        };

        var sender = new ApplicationUser
        {
            Id = creatorId,
            UserName = "creator1",
            DisplayName = "Creator",
            Email = "creator@test.com",
            EmailConfirmed = true
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender },
            Messages = new List<ChatMessage>()
        };

        var createdMessage = new ChatMessage
        {
            Id = "msg1",
            Content = "New message content",
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = conversationId,
            SenderId = creatorId,
            Sender = sender
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(creatorId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, creatorId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.Add(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()))
            .Callback<ChatMessage, ApplicationDbContext>((msg, db) =>
            {
                msg.Id = "msg1";
            });

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMessage);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("msg1", result.Data.Id);
        Assert.Equal("New message content", result.Data.Content);

        _chatMessageRepositoryMock.Verify(r => r.Add(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithNonExistentSender_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = "nonexistent",
            ConversationId = "conv1",
            Content = "New message content"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender user not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = "creator1",
            ConversationId = "conv1",
            Content = "" // Empty content should fail validation
        };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Content", "Content is required")
        });

        _createChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Content is required", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateChatMessageAsync Tests

    [Fact]
    public async Task UpdateChatMessageAsync_WithValidDto_ShouldUpdateChatMessage()
    {
        // Arrange
        string creatorId = "creator1";
        string messageId = "msg1";

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = messageId,
            CreatorUserId = creatorId,
            Content = "Updated message content"
        };

        var sender = new ApplicationUser
        {
            Id = creatorId,
            UserName = "creator1",
            DisplayName = "Creator",
            Email = "creator@test.com",
            EmailConfirmed = true
        };

        var existingMessage = new ChatMessage
        {
            Id = messageId,
            Content = "Original content",
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "conv1",
            SenderId = creatorId,
            Sender = sender
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(creatorId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMessage);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", creatorId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Updated message content", existingMessage.Content);
        Assert.NotNull(existingMessage.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateChatMessageAsync_WithNonExistentMessage_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "nonexistent",
            CreatorUserId = "creator1",
            Content = "Updated content"
        };

        var sender = new ApplicationUser
        {
            Id = "creator1",
            UserName = "creator1",
            DisplayName = "Creator",
            Email = "creator@test.com",
            EmailConfirmed = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("creator1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Chat message not found or user is not the sender", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteChatMessageAsync Tests

    [Fact]
    public async Task DeleteChatMessageAsync_WithValidMessage_ShouldDeleteChatMessage()
    {
        // Arrange
        string messageId = "msg1";
        string userId = "user1";

        var message = new ChatMessage
        {
            Id = messageId,
            Content = "Test message",
            CreatedAtUtc = DateTime.UtcNow,
            ConversationId = "conv1",
            SenderId = userId
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Title = "Test Conversation",
            Type = ConversationType.Team,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>
            {
                new() { Id = userId, UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true }
            },
            Messages = new List<ChatMessage> { message }
        };

        var sender = new ApplicationUser
        {
            Id = userId,
            UserName = "user1",
            DisplayName = "User 1",
            Email = "user1@test.com",
            EmailConfirmed = true
        };

        message.Sender = sender; // Add the Sender navigation property

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message); // For the second call in the method

        // Act
        Result<bool> result = await _chatMessageService.DeleteChatMessageAsync(messageId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Data);

        _chatMessageRepositoryMock.Verify(r => r.Remove(message, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteChatMessageAsync_WithNonExistentMessage_ShouldReturnFailure()
    {
        // Arrange
        string messageId = "nonexistent";
        string userId = "user1";

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync(messageId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage?)null);

        // Act
        Result<bool> result = await _chatMessageService.DeleteChatMessageAsync(messageId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Chat message not found or user is not the sender", result.ErrorMessages[0]);
    }

    #endregion
}