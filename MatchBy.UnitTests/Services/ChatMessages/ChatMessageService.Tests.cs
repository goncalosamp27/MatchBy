using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.ChatMessages;

public class ChatMessageServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateChatMessageDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateChatMessageDto>> _updateValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ChatMessageService _chatMessageService;

    public ChatMessageServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateChatMessageDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateChatMessageDto>>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _chatMessageService = new ChatMessageService(
            _dbContext,
            _createValidatorMock.Object,
            _updateValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetChatMessagesAsync Tests

    [Fact]
    public async Task GetChatMessagesAsync_WithValidConversation_ShouldReturnMessages()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("conv1", "user1", 10, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("msg1", result.Data.Data[0].Id);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> result = await _chatMessageService.GetChatMessagesAsync("conv1", "user2", 10, null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not a participant", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message1 = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Message 1",
            CreatedAtUtc = DateTime.UtcNow
        };

        var message2 = new ChatMessage
        {
            Id = "msg2",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Message 2",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(1)
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddRangeAsync(message1, message2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<CursorPaginationResponse<List<ChatMessageDto>>> firstPage = await _chatMessageService.GetChatMessagesAsync("conv1", "user1", 1, null);
        Result<CursorPaginationResponse<List<ChatMessageDto>>> secondPage = await _chatMessageService.GetChatMessagesAsync("conv1", "user1", 1, firstPage.Data!.NextCursor);

        // Assert
        Assert.True(firstPage.Success);
        Assert.True(secondPage.Success);
        Assert.NotNull(firstPage.Data.NextCursor);
        Assert.Single(firstPage.Data.Data);
        Assert.Single(secondPage.Data!.Data);
    }

    #endregion

    #region GetChatMessageByIdAsync Tests

    [Fact]
    public async Task GetChatMessageByIdAsync_WithValidId_ShouldReturnMessage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync("msg1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("msg1", result.Data.Id);
    }

    [Fact]
    public async Task GetChatMessageByIdAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.GetChatMessageByIdAsync("msg1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not a participant", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateChatMessageAsync Tests

    [Fact]
    public async Task CreateChatMessageAsync_WithValidData_ShouldCreateMessage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Content = "Hello World"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Hello World", result.Data.Content);
        
        Conversation? updatedConversation = await _dbContext.Conversations.FindAsync("conv1");
        Assert.NotNull(updatedConversation!.LastMessageAtUtc);
        Assert.Equal("Hello World", updatedConversation.LastMessageContent);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Content = ""
        };

        var validationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("Content", "Content is required") });

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateChatMessageAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };
        var user2 = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2",
            DisplayName = "User Two",
            Email = "user2@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };

        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Users.AddAsync(user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateChatMessageDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user2",
            Content = "Hello"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.CreateChatMessageAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Conversation not found or user is not a participant.", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateChatMessageAsync Tests

    [Fact]
    public async Task UpdateChatMessageAsync_WithValidData_ShouldUpdateMessage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Old Content",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg1",
            CreatorUserId = "user1",
            Content = "New Content"
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Content", result.Data!.Content);
    }

    [Fact]
    public async Task UpdateChatMessageAsync_WithNonSender_ShouldReturnFailure()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };
        
        
        var user2 = new ApplicationUser
        {
            Id = "user2",
            UserName = "user2",
            DisplayName = "User Two",
            Email = "user2@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Old Content",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Users.AddAsync(user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg1",
            CreatorUserId = "user2",
            Content = "New Content"
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateChatMessageDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ChatMessageDto> result = await _chatMessageService.UpdateChatMessageAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Chat message not found or user is not the sender.", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteChatMessageAsync Tests

    [Fact]
    public async Task DeleteChatMessageAsync_WithNonSender_ShouldReturnFailure()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "user1",
            DisplayName = "User One",
            Email = "user1@test.com"
        };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var message = new ChatMessage
        {
            Id = "msg1",
            ConversationId = "conv1",
            SenderId = "user1",
            Content = "Hello",
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _chatMessageService.DeleteChatMessageAsync("msg1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not the sender", result.ErrorMessages[0]);
    }

    #endregion
}


