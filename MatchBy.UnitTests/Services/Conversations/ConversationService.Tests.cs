using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.S3;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.Conversations;

public class ConversationServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateConversationDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateConversationDto>> _updateValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ConversationService _conversationService;

    public ConversationServiceTests()
    {
        var s3ServiceMock = new Mock<IS3Service>();
        _createValidatorMock = new Mock<IValidator<CreateConversationDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateConversationDto>>();
        var imageRefreshServiceMock = new Mock<IImageRefreshService>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _conversationService = new ConversationService(
            _dbContext,
            s3ServiceMock.Object,
            _createValidatorMock.Object,
            _updateValidatorMock.Object,
            imageRefreshServiceMock.Object);
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
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
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
        Result<CursorPaginationResponse<List<ConversationDto>>> result = await _conversationService.GetConversationsAsync("user1", 10, null, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("conv1", result.Data.Data[0].Id);
    }

    [Fact]
    public async Task GetConversationsAsync_WithQuery_ShouldFilterConversations()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        var conversation1 = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            Title = "Basketball Team",
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        var conversation2 = new Conversation
        {
            Id = "conv2",
            Type = ConversationType.Team,
            Title = "Football Team",
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddRangeAsync(conversation1, conversation2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<CursorPaginationResponse<List<ConversationDto>>> result = await _conversationService.GetConversationsAsync("user1", 10, null, "Basketball");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Data);
        Assert.Equal("Basketball Team", result.Data.Data[0].Title);
    }

    [Fact]
    public async Task GetConversationsAsync_WithCursor_ShouldReturnNextPage()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
        };

        var conversation1 = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            Participants = new List<ApplicationUser> { user }
        };

        var conversation2 = new Conversation
        {
            Id = "conv2",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddRangeAsync(conversation1, conversation2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<CursorPaginationResponse<List<ConversationDto>>> firstPage = await _conversationService.GetConversationsAsync("user1", 1, null, null);
        Result<CursorPaginationResponse<List<ConversationDto>>> secondPage = await _conversationService.GetConversationsAsync("user1", 1, firstPage.Data!.NextCursor, null);

        // Assert
        Assert.True(firstPage.Success);
        Assert.True(secondPage.Success);
        Assert.NotNull(firstPage.Data.NextCursor);
        Assert.Single(firstPage.Data.Data);
        Assert.Single(secondPage.Data!.Data);
    }

    #endregion

    #region GetConversationByIdAsync Tests

    [Fact]
    public async Task GetConversationByIdAsync_WithValidId_ShouldReturnConversation()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com"
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
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync("conv1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("conv1", result.Data.Id);
    }

    [Fact]
    public async Task GetConversationByIdAsync_WithInvalidId_ShouldReturnFailure()
    {
        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync("nonexistent", "user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No conversation found", result.ErrorMessages);
    }

    [Fact]
    public async Task GetConversationByIdAsync_WithPrivateConversation_ShouldSetTitleToOtherParticipant()
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
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<ConversationDto> result = await _conversationService.GetConversationByIdAsync("conv1", "user1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("User Two", result.Data!.Title);
    }

    #endregion

    #region CreateConversationAsync Tests

    [Fact]
    public async Task CreateConversationAsync_WithValidData_ShouldCreateConversation()
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

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user1", "user2" }
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(ConversationType.Private, result.Data.Type);
    }

    [Fact]
    public async Task CreateConversationAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user1" }
        };

        var validationResult = new FluentValidation.Results.ValidationResult(
            new[] { new FluentValidation.Results.ValidationFailure("ParticipantIds", "Must have 2 participants") });

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessages);
    }

    [Fact]
    public async Task CreateConversationAsync_WithExistingPrivateConversation_ShouldReturnFailure()
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

        var existingConversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.Conversations.AddAsync(existingConversation);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user1",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user1", "user2" }
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ConversationDto> result = await _conversationService.CreateConversationAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
    }

    #endregion

    #region UpdateConversationAsync Tests

    [Fact]
    public async Task UpdateConversationAsync_WithValidData_ShouldUpdateConversation()
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
            Type = ConversationType.Team,
            Title = "Old Title",
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateConversationDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user1",
            Title = "New Title",
            ParticipantIds = new List<string> { "user1" }
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Title", result.Data!.Title);
    }

    [Fact]
    public async Task UpdateConversationAsync_WithNonCreator_ShouldReturnFailure()
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
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateConversationDto
        {
            ConversationId = "conv1",
            CreatorUserId = "user2",
            Title = "New Title",
            ParticipantIds = new List<string> { "user2" }
        };

        _updateValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<ConversationDto> result = await _conversationService.UpdateConversationAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not the creator", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteConversationAsync Tests
    
    [Fact]
    public async Task DeleteConversationAsync_WithNonAuthorizedUser_ShouldReturnFailure()
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
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _conversationService.DeleteConversationAsync("conv1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveConversationAsync Tests

    [Fact]
    public async Task LeaveConversationAsync_WithParticipant_ShouldRemoveParticipant()
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
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync("conv1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data); // Returns 1 when soft-deleted, because only 1 participant remains
    }

    [Fact]
    public async Task LeaveConversationAsync_WithLastParticipant_ShouldSoftDelete()
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
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync("conv1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.Data); // Returns 1 when soft-deleted
        
        Conversation? updatedConversation = await _dbContext.Conversations.FindAsync("conv1");
        Assert.NotNull(updatedConversation!.DeletedAtUtc);
    }

    [Fact]
    public async Task LeaveConversationAsync_WithNonParticipant_ShouldReturnFailure()
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
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user }
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<int> result = await _conversationService.LeaveConversationAsync("conv1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not a participant", result.ErrorMessages[0]);
    }

    #endregion
}


