using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.ChatMessage;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.S3;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Conversations;

public class ConversationServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateConversationDto>> _createConversationValidatorMock;
    private readonly Mock<IValidator<UpdateConversationDto>> _updateConversationValidatorMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IChatMessageRepository> _chatMessageRepositoryMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ConversationService _conversationService;

    public ConversationServiceTests()
    {
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        var s3ServiceMock = new Mock<IS3Service>();
        _createConversationValidatorMock = new Mock<IValidator<CreateConversationDto>>();
        _updateConversationValidatorMock = new Mock<IValidator<UpdateConversationDto>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();

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
        _createConversationValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _updateConversationValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _conversationService = new ConversationService(
            dbContextFactoryMock.Object,
            s3ServiceMock.Object,
            _createConversationValidatorMock.Object,
            _updateConversationValidatorMock.Object,
            _userRepositoryMock.Object,
            _chatMessageRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            _imageRefreshServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetConversationsAsync Tests

    [Fact]
    public async Task GetConversationsAsync_WithValidParameters_ShouldReturnConversations()
    {
        // Arrange
        string creatorUserId = "user1";
        int pageSize = 10;
        string? cursor = null;
        string? query = null;

        var conversations = new List<Conversation>
        {
            new Conversation
            {
                Id = "conv1",
                Type = ConversationType.Private,
                Title = "Test Conversation",
                CreatorId = creatorUserId,
                Participants = new List<ApplicationUser>
                {
                    new() { Id = creatorUserId, UserName = "user1", DisplayName = "User 1" },
                    new() { Id = "user2", UserName = "user2", DisplayName = "User 2" }
                },
                Messages = new List<ChatMessage>(),
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        var cursorResponse = new CursorPaginationResponse<List<Conversation>>
        {
            Data = conversations,
            NextCursor = null
        };

        _conversationRepositoryMock
            .Setup(r => r.GetConversationsForUserAsync(creatorUserId, pageSize, cursor, query, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorResponse);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<CursorPaginationResponse<List<ConversationDto>>> result = await _conversationService.GetConversationsAsync(creatorUserId, pageSize, cursor, query);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("conv1", result.Data.Data[0].Id);

        _conversationRepositoryMock.Verify(r => r.GetConversationsForUserAsync(creatorUserId, pageSize, cursor, query, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()), Times.Once);
    }

    #endregion

    #region GetConversationByIdAsync Tests

    [Fact]
    public async Task GetConversationByIdAsync_WithValidId_ShouldReturnConversation()
    {
        // Arrange
        string conversationId = "conv1";
        string creatorUserId = "user1";

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Private,
            Title = "Test Conversation",
            CreatorId = creatorUserId,
            Participants = new List<ApplicationUser>
            {
                new() { Id = creatorUserId, UserName = "user1", DisplayName = "User 1" },
                new() { Id = "user2", UserName = "user2", DisplayName = "User 2" }
            },
            Messages = new List<ChatMessage>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, creatorUserId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshConversationImagesAsync(conversation))
            .Returns(Task.CompletedTask);

        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync(conversationId, creatorUserId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(conversationId, result.Data.Id);

        _conversationRepositoryMock.Verify(r => r.GetByIdAsync(conversationId, creatorUserId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageRefreshServiceMock.Verify(s => s.RefreshConversationImagesAsync(conversation), Times.Once);
    }

    [Fact]
    public async Task GetConversationByIdAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "nonexistent";
        string creatorUserId = "user1";

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, creatorUserId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync(conversationId, creatorUserId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No conversation found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateConversationAsync Tests

    [Fact]
    public async Task CreateConversationAsync_WithValidDto_ShouldCreateConversation()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            ConversationType = ConversationType.Private,
            Title = "New Conversation",
            CreatorUserId = "user1",
            ParticipantIds = new List<string> { "user2" }
        };

        var participants = new List<ApplicationUser>
        {
            new() { Id = "user1", UserName = "user1" },
            new() { Id = "user2", UserName = "user2" }
        };

        var createdConversation = new Conversation
        {
            Id = "new-conv-id",
            Type = ConversationType.Private,
            Title = "New Conversation",
            CreatorId = "user1",
            Participants = participants,
            Messages = new List<ChatMessage>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _userRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConversation);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        _conversationRepositoryMock.Verify(r => r.PrivateConversationExistsAsync(It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.GetUsersByIdsAsync(It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversationRepositoryMock.Verify(r => r.Add(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateConversationAsync_WithValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            ConversationType = ConversationType.Private,
            CreatorUserId = "user1",
            ParticipantIds = new List<string>()
        };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("ParticipantIds", "At least one participant is required")
        });

        _createConversationValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("At least one participant is required", result.ErrorMessages);
    }

    [Fact]
    public async Task CreateConversationAsync_WithExistingPrivateConversation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            ConversationType = ConversationType.Private,
            CreatorUserId = "user1",
            ParticipantIds = new List<string> { "user2" }
        };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(It.IsAny<List<string>>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("existing-conv-id");

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Private conversation between these users already exists", result.ErrorMessages[0]);
    }

    #endregion

    #region PrivateConversationExists Tests

    [Fact]
    public async Task PrivateConversationExists_WithExistingConversation_ShouldReturnSuccess()
    {
        // Arrange
        var participantIds = new List<string> { "user1", "user2" };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(participantIds, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("existing-conv-id");

        // Act
        Result<string> result = await _conversationService.PrivateConversationExists(participantIds);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("existing-conv-id", result.Data);
    }

    [Fact]
    public async Task PrivateConversationExists_WithNoExistingConversation_ShouldReturnFailure()
    {
        // Arrange
        var participantIds = new List<string> { "user1", "user2" };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(participantIds, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        Result<string> result = await _conversationService.PrivateConversationExists(participantIds);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No private conversation exists between these users", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateConversationAsync Tests

    [Fact]
    public async Task UpdateConversationAsync_WithValidDto_ShouldUpdateConversation()
    {
        // Arrange
        var updateDto = new UpdateConversationDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Title = "Updated Title",
            ParticipantIds = new List<string> { "user1", "user2" }
        };

        var existingConversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Match,
            Title = "Old Title",
            CreatorId = "user1",
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1", DisplayName = "User 1" }
            },
            Messages = new List<ChatMessage>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        var updatedParticipants = new List<ApplicationUser>
        {
            new() { Id = "user1", UserName = "user1", DisplayName = "User 1" },
            new() { Id = "user2", UserName = "user2", DisplayName = "User 2" }
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConversation);

        _userRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(updateDto.ParticipantIds, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedParticipants);

        _imageRefreshServiceMock
            .Setup(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);

        _conversationRepositoryMock.Verify(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2)); // Called once in UpdateConversationAsync and once in GetConversationByIdAsync
        _userRepositoryMock.Verify(r => r.GetUsersByIdsAsync(updateDto.ParticipantIds, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConversationAsync_WithValidationFailure_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateConversationDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Title = "",
            ParticipantIds = new List<string>()
        };

        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Title", "Title is required")
        });

        _updateConversationValidatorMock
            .Setup(v => v.ValidateAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Title is required", result.ErrorMessages);
    }

    [Fact]
    public async Task UpdateConversationAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateConversationDto
        {
            ConversationId = "nonexistent",
            CreatorUserId = "user1",
            Title = "Updated Title",
            ParticipantIds = new List<string> { "user1", "user2" }
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Conversation not found or user is not the creator", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteConversationAsync Tests

    [Fact]
    public async Task DeleteConversationAsync_WithValidPermissions_ShouldDeleteConversation()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Match,
            CreatorId = userId,
            Participants = new List<ApplicationUser>
            {
                new() { Id = userId, UserName = "user1", DisplayName = "User 1" }
            },
            Messages = new List<ChatMessage>
            {
                new() { Id = "msg1", Content = "Test message" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _conversationRepositoryMock
            .Setup(r => r.CanDeleteConversation(conversationId, ConversationType.Match, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync(conversationId, userId);

        // Assert
        Assert.True(result.Success);

        _conversationRepositoryMock.Verify(r => r.Remove(conversation, It.IsAny<ApplicationDbContext>()), Times.Once);
        _chatMessageRepositoryMock.Verify(r => r.Remove(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_WithNoPermissions_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Match,
            CreatorId = "other-user",
            Participants = new List<ApplicationUser>
            {
                new() { Id = "other-user", UserName = "other" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _conversationRepositoryMock
            .Setup(r => r.CanDeleteConversation(conversationId, ConversationType.Match, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync(conversationId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User does not have permission to delete this conversation", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task DeleteConversationAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "nonexistent";
        string userId = "user1";

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync(conversationId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Conversation not found", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveConversationAsync Tests

    [Fact]
    public async Task LeaveConversationAsync_WithValidUser_ShouldRemoveUser()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1"
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Match,
            Participants = new List<ApplicationUser>
            {
                user,
                new() { Id = "user2", UserName = "user2" }
            },
            Messages = new List<ChatMessage>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync(conversationId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data); // User removed, conversation not deleted
        Assert.DoesNotContain(user, conversation.Participants);
    }

    [Fact]
    public async Task LeaveConversationAsync_WithLastParticipant_ShouldDeleteConversation()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "user1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "user1"
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Match,
            Participants = new List<ApplicationUser> { user },
            Messages = new List<ChatMessage>
            {
                new() { Id = "msg1", Content = "Test message" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync(conversationId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data); // Conversation deleted because no participants remain

        _conversationRepositoryMock.Verify(r => r.Remove(conversation, It.IsAny<ApplicationDbContext>()), Times.Once);
        _chatMessageRepositoryMock.Verify(r => r.Remove(It.IsAny<ChatMessage>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task LeaveConversationAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "conv1";
        string userId = "nonparticipant";

        var conversation = new Conversation
        {
            Id = conversationId,
            Type = ConversationType.Match,
            Participants = new List<ApplicationUser>
            {
                new() { Id = "user1", UserName = "user1" },
                new() { Id = "user2", UserName = "user2" }
            },
            CreatedAtUtc = DateTime.UtcNow
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync(conversationId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User is not a participant of the conversation", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task LeaveConversationAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        string conversationId = "nonexistent";
        string userId = "user1";

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync(conversationId, userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync(conversationId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Conversation not found", result.ErrorMessages[0]);
    }

    #endregion
}