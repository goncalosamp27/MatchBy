using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.ChatMessage;
using MatchBy.Repositories.User;
using MatchBy.Services.ChatMessages;
using Microsoft.EntityFrameworkCore;
using Moq;
using ChatMessage = MatchBy.Models.ChatMessage;

namespace MatchBy.UnitTests.Services.ChatMessages;

public class ChatMessageServiceTests : IDisposable
{
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
    private readonly Mock<IValidator<CreateChatMessageDto>> _createChatMessageValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ChatMessageService _chatMessageService;

    public ChatMessageServiceTests()
    {
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
        _createChatMessageValidatorMock = new Mock<IValidator<CreateChatMessageDto>>();
        var updateChatMessageValidatorMock = new Mock<IValidator<UpdateChatMessageDto>>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validators to return valid by default
        _createChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        updateChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _chatMessageService = new ChatMessageService(
            _conversationRepositoryMock.Object,
            _userRepositoryMock.Object,
            _chatMessageRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createChatMessageValidatorMock.Object,
            updateChatMessageValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetChatMessagesAsync Tests

    [Fact]
    public async Task GetChatMessagesAsync_WithValidConversationAndParticipant_ShouldReturnMessages()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        var messages = new List<ChatMessage>
        {
            new() { Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "msg2", ConversationId = "conv1", SenderId = "user2", Content = "Hi", CreatedAtUtc = DateTime.UtcNow }
        };

        var cursorPaginationResponse = new CursorPaginationResponse<List<ChatMessage>>
        {
            Data = messages,
            NextCursor = null
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.GetChatMessagesAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), 10, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPaginationResponse);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("conv1", "user1", pageSize: 10, cursor: null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data.Count);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("nonexistent", "user1", pageSize: 10, cursor: null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithNonParticipant_ShouldReturnFailure()
    {
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user3", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("conv1", "user3", pageSize: 10, cursor: null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithCursor_ShouldReturnMessagesAfterCursor()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        var messages = new List<ChatMessage>
        {
            new() { Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Message 1", CreatedAtUtc = DateTime.UtcNow }
        };

        var cursorPaginationResponse = new CursorPaginationResponse<List<ChatMessage>>
        {
            Data = messages,
            NextCursor = "msg1"
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.GetChatMessagesAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), 10, "msg2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPaginationResponse);

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("conv1", "user1", pageSize: 10, cursor: "msg2");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("msg1", result.Data.NextCursor);
    }

    #endregion

    #region GetChatMessageByIdAsync Tests

    [Fact]
    public async Task GetChatMessageByIdAsync_WithValidIdAndParticipant_ShouldReturnMessage()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [user1, user2]
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync("msg1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("msg1", result.Data.Id);
        Assert.Equal("Hello", result.Data.Content);
    }

    [Fact]
    public async Task GetChatMessageByIdAsync_WithNonExistentMessage_ShouldReturnFailure()
    {
        // Arrange
        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync("nonexistent", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessageByIdAsync_WithNonParticipant_ShouldReturnFailure()
    {
        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user3", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync("msg1", "user3");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateChatMessageAsync Tests

    [Fact]
    public async Task CreateChatMessageAsync_WithValidDto_ShouldCreateMessage()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var participant = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, participant }
        };

        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Content = "Hello"
        };

        var createdMessage = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _chatMessageRepositoryMock
            .Setup(r => r.Add(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()))
            .Callback<ChatMessage, ApplicationDbContext>((m, db) =>
            {
                m.Id = "msg1";
            });

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdMessage);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Hello", result.Data.Content);
        _chatMessageRepositoryMock.Verify(r => r.Add(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Content = "" // Invalid empty content
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("Content", "Content is required")
        });

        _createChatMessageValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        _chatMessageRepositoryMock.Verify(r => r.Add(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithNonExistentSender_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "nonexistent",
            Content = "Hello"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateChatMessageAsync Tests

    [Fact]
    public async Task UpdateChatMessageAsync_WithValidDto_ShouldUpdateMessage()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var participant = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, participant }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Original",
            CreatedAtUtc = DateTime.UtcNow
        };

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg1",
            CreatorUserId = "user1",
            Content = "Updated"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Updated", message.Content);
        Assert.NotNull(message.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateChatMessageAsync_WithNonExistentMessage_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "nonexistent",
            CreatorUserId = "user1",
            Content = "Updated"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", EmailConfirmed = true });

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatMessage?)null);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteChatMessageAsync Tests

    [Fact]
    public async Task DeleteChatMessageAsync_WithValidMessageAndSender_ShouldDeleteMessage()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 },
            Messages = new List<ChatMessage>
            {
                new() { Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Message 1", CreatedAtUtc = DateTime.UtcNow },
                new() { Id = "msg2", ConversationId = "conv1", SenderId = "user2", Content = "Message 2", CreatedAtUtc = DateTime.UtcNow }
            }
        };

        ChatMessage message = conversation.Messages.First(m => m.Id == "msg1");

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<bool> result = await _chatMessageService.DeleteChatMessageAsync("msg1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _chatMessageRepositoryMock.Verify(r => r.Remove(message, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteChatMessageAsync_WithWrongSender_ShouldReturnFailure()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        _chatMessageRepositoryMock
            .Setup(r => r.GetByIdAsync("msg1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(message);

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<bool> result = await _chatMessageService.DeleteChatMessageAsync("msg1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not authorized", result.ErrorMessages[0]);
    }

    #endregion
}
