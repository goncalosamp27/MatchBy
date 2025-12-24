using System.Collections.ObjectModel;
using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Enums;
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
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IConversationRepository> _conversationRepositoryMock;
    private readonly Mock<IValidator<CreateConversationDto>> _createConversationValidatorMock;
    private readonly Mock<IImageRefreshService> _imageRefreshServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ConversationService _conversationService;

    public ConversationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        var chatMessageRepositoryMock = new Mock<IChatMessageRepository>();
        _conversationRepositoryMock = new Mock<IConversationRepository>();
        var s3ServiceMock = new Mock<IS3Service>();
        _createConversationValidatorMock = new Mock<IValidator<CreateConversationDto>>();
        var updateConversationValidatorMock = new Mock<IValidator<UpdateConversationDto>>();
        _imageRefreshServiceMock = new Mock<IImageRefreshService>();
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
        _createConversationValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        updateConversationValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Setup image refresh service
        _imageRefreshServiceMock
            .Setup(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);

        _conversationService = new ConversationService(
            dbContextFactoryMock.Object,
            s3ServiceMock.Object,
            _createConversationValidatorMock.Object,
            updateConversationValidatorMock.Object,
            _userRepositoryMock.Object,
            chatMessageRepositoryMock.Object,
            _conversationRepositoryMock.Object,
            _imageRefreshServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetConversationsAsync Tests

    [Fact]
    public async Task GetConversationsAsync_WithValidUserId_ShouldReturnConversations()
    {
        // Arrange
        var conversations = new List<Conversation>
        {
            new() { Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow, Participants = [] },
            new() { Id = "conv2", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow, Participants = [] }
        };

        var cursorPaginationResponse = new CursorPaginationResponse<List<Conversation>>
        {
            Data = conversations,
            NextCursor = null
        };

        _conversationRepositoryMock
            .Setup(r => r.GetConversationsForUserAsync("user1", 10, null, null, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPaginationResponse);

        // Act
        Result<CursorPaginationResponse<List<ConversationDto>>> result = await _conversationService.GetConversationsAsync("user1", pageSize: 10, cursor: null, query: null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data.Count);
        _imageRefreshServiceMock.Verify(s => s.RefreshConversationImagesAsync(It.IsAny<Conversation>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetConversationsAsync_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        var conversations = new List<Conversation>
        {
            new() { Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow, Participants = [] }
        };

        var cursorPaginationResponse = new CursorPaginationResponse<List<Conversation>>
        {
            Data = conversations,
            NextCursor = "conv1"
        };

        _conversationRepositoryMock
            .Setup(r => r.GetConversationsForUserAsync("user1", 10, "conv2", null, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cursorPaginationResponse);

        // Act
        Result<CursorPaginationResponse<List<ConversationDto>>> result = await _conversationService.GetConversationsAsync("user1", pageSize: 10, cursor: "conv2", query: null);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("conv1", result.Data.NextCursor);
    }

    #endregion

    #region GetConversationByIdAsync Tests

    [Fact]
    public async Task GetConversationByIdAsync_WithValidIdAndParticipant_ShouldReturnConversation()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = []
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync("conv1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("conv1", result.Data.Id);
        _imageRefreshServiceMock.Verify(s => s.RefreshConversationImagesAsync(conversation), Times.Once);
    }

    [Fact]
    public async Task GetConversationByIdAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync("nonexistent", "user1");

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
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = ["user1", "user2"]
        };

        var createdConversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [user1, user2]
        };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(new List<string>{"user1", "user2"}, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync( new List<string>(), It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([user1, user2]);

        _conversationRepositoryMock
            .Setup(r => r.Add(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()))
            .Callback<Conversation, ApplicationDbContext>((c, db) =>
            {
                c.Id = "conv1";
                c.Participants = [user1, user2];
            });

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConversation);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        _conversationRepositoryMock.Verify(r => r.Add(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateConversationAsync_WithExistingPrivateConversation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = ["user1", "user2"]
        };

        _conversationRepositoryMock
            .Setup(r => r.PrivateConversationExistsAsync(new List<string>{"user1", "user2"}, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
        _conversationRepositoryMock.Verify(r => r.Add(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task CreateConversationAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = []
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("ParticipantIds", "At least one participant is required")
        });

        _createConversationValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        _conversationRepositoryMock.Verify(r => r.Add(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion

    #region UpdateConversationAsync Tests

    [Fact]
    public async Task UpdateConversationAsync_WithValidDto_ShouldUpdateConversation()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            Title = "Old Title",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [user1]
        };

        var updateDto = new UpdateConversationDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Title = "New Title",
            ParticipantIds = ["user1", "user2"]
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _userRepositoryMock
            .Setup(r => r.GetUsersByIdsAsync(new List<string>{"user1", "user2"}, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([user1, user2]);

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Title", conversation.Title);
    }

    [Fact]
    public async Task UpdateConversationAsync_WithNonExistentConversation_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateConversationDto
        {
            ConversationId = "nonexistent",
            CreatorUserId = "user1",
            Title = "New Title",
            ParticipantIds = ["user1"]
        };

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Conversation?)null);

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteConversationAsync Tests

    [Fact]
    public async Task DeleteConversationAsync_WithValidConversationAndPermission_ShouldDeleteConversation()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = [],
            Messages = []
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        _conversationRepositoryMock
            .Setup(r => r.CanDeleteConversation("conv1", ConversationType.Private, "user1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync("conv1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _conversationRepositoryMock.Verify(r => r.Remove(It.IsAny<Conversation>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_WithoutPermission_ShouldReturnFailure()
    {
        // Arrange
        _conversationRepositoryMock
            .Setup(r => r.CanDeleteConversation("conv1", ConversationType.Team, "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync("conv1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveConversationAsync Tests

    [Fact]
    public async Task LeaveConversationAsync_WithValidConversation_ShouldRemoveParticipant()
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

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        _conversationRepositoryMock
            .Setup(r => r.GetByIdAsync("conv1", "user2", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync("conv1", "user2");

        // Assert
        Assert.True(result.Success);
    }

    #endregion
}
