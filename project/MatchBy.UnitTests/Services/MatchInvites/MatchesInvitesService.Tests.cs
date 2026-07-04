using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.MatchInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.MatchInvites;

public class MatchesInvitesServiceTests : IDisposable
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<IChatMessageService> _chatMessageServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IMatchInviteRepository> _matchInviteRepositoryMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly Mock<IValidator<CreateMatchInviteDto>> _createInviteValidatorMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesInvitesService _matchesInvitesService;

    public MatchesInvitesServiceTests()
    {
        _conversationServiceMock = new Mock<IConversationService>();
        _chatMessageServiceMock = new Mock<IChatMessageService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _matchInviteRepositoryMock = new Mock<IMatchInviteRepository>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _createInviteValidatorMock = new Mock<IValidator<CreateMatchInviteDto>>();
        _notificationServiceMock = new Mock<INotificationService>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validator to return valid by default
        _createInviteValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _matchesInvitesService = new MatchesInvitesService(
            _conversationServiceMock.Object,
            _chatMessageServiceMock.Object,
            _userRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _matchInviteRepositoryMock.Object,
            _dbContextFactoryMock.Object,
            _createInviteValidatorMock.Object,
            _notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetMatchInvite Tests

    [Fact]
    public async Task GetMatchInvite_WithExistingInvite_ShouldReturnInviteDto()
    {
        // Arrange
        string matchId = "match1";
        string receiverId = "user1";

        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = matchId,
            ReceiverId = receiverId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, receiverId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetMatchInvite(matchId, receiverId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("invite1", result.Data.Id);
        Assert.Equal(InviteStatus.Pending, result.Data.Status);
    }

    [Fact]
    public async Task GetMatchInvite_WithNonExistentInvite_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "match1";
        string receiverId = "user1";

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(matchId, receiverId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchInvite?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetMatchInvite(matchId, receiverId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No invite found for match match1 and receiver user1", result.ErrorMessages[0]);
    }

    #endregion

    #region GetInviteById Tests

    [Fact]
    public async Task GetInviteById_WithValidId_ShouldReturnInviteDto()
    {
        // Arrange
        string inviteId = "invite1";

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = "user1",
            SenderId = "sender1",
            Status = InviteStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetInviteById(inviteId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(inviteId, result.Data.Id);
        Assert.Equal(InviteStatus.Accepted, result.Data.Status);
    }

    [Fact]
    public async Task GetInviteById_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "nonexistent";

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchInvite?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetInviteById(inviteId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invite with id nonexistent not found", result.ErrorMessages[0]);
    }

    #endregion

    #region GetReceivedInvites Tests

    [Fact]
    public async Task GetReceivedInvites_WithValidUserId_ShouldReturnInvites()
    {
        // Arrange
        string userId = "user1";
        int page = 1;
        int pageSize = 10;

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", ReceiverId = userId, SenderId = "sender1", Status = InviteStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<MatchInvite>>
        {
            Data = invites,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetReceivedInvites(userId, It.IsAny<ApplicationDbContext>(), page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchInviteDto>>> result = await _matchesInvitesService.GetReceivedInvites(userId, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("invite1", result.Data.Data[0].Id);
    }

    #endregion

    #region GetSentInvites Tests

    [Fact]
    public async Task GetSentInvites_WithValidUserId_ShouldReturnInvites()
    {
        // Arrange
        string userId = "user1";
        int page = 1;
        int pageSize = 10;

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", SenderId = userId, ReceiverId = "receiver1", Status = InviteStatus.Accepted, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<MatchInvite>>
        {
            Data = invites,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetSentInvites(userId, It.IsAny<ApplicationDbContext>(), page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchInviteDto>>> result = await _matchesInvitesService.GetSentInvites(userId, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("invite1", result.Data.Data[0].Id);
        Assert.Equal(InviteStatus.Accepted, result.Data.Data[0].Status);
    }

    #endregion

    #region GetInvitesForMatch Tests

    [Fact]
    public async Task GetInvitesForMatch_WithValidMatchId_ShouldReturnInvites()
    {
        // Arrange
        string matchId = "match1";
        int page = 1;
        int pageSize = 10;

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", MatchId = matchId, ReceiverId = "user1", SenderId = "sender1", Status = InviteStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<MatchInvite>>
        {
            Data = invites,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(matchId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _matchInviteRepositoryMock
            .Setup(r => r.GetInvitesForMatch(matchId, It.IsAny<ApplicationDbContext>(), page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchInviteDto>>> result = await _matchesInvitesService.GetInvitesForMatch(matchId, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("invite1", result.Data.Data[0].Id);
    }

    [Fact]
    public async Task GetInvitesForMatch_WithNonExistentMatch_ShouldReturnFailure()
    {
        // Arrange
        string matchId = "nonexistent";

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync(matchId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<PaginationResponse<List<MatchInviteDto>>> result = await _matchesInvitesService.GetInvitesForMatch(matchId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Match with id nonexistent not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_WithValidDto_ShouldCreateInviteAndSendNotification()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join my match!"
        };

        var sender = new ApplicationUser
        {
            Id = "sender1",
            UserName = "sender",
            DisplayName = "Sender",
            Email = "sender@test.com",
            EmailConfirmed = true
        };

        var receiver = new ApplicationUser
        {
            Id = "receiver1",
            UserName = "receiver",
            DisplayName = "Receiver",
            Email = "receiver@test.com",
            EmailConfirmed = true
        };

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            CreatorId = "sender1",
            Participants = new List<ApplicationUser>(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchInvite?)null);

        _conversationServiceMock
            .Setup(s => s.PrivateConversationExists(It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Fail("No conversation exists"));

        _conversationServiceMock
            .Setup(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(new ConversationDto
            {
                Id = "conv1",
                Type = ConversationType.Private,
                CreatorId = "sender1",
                CreatedAtUtc = DateTime.UtcNow,
                MessagesCount = 0
            }));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        var createdInvite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.Add(It.IsAny<MatchInvite>(), It.IsAny<ApplicationDbContext>()))
            .Callback<MatchInvite, ApplicationDbContext>((invite, db) =>
            {
                invite.Id = "invite1";
            });

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInvite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.True(result.Success, $"CreateInvite failed: {string.Join(", ", result.ErrorMessages)}");
        Assert.NotNull(result.Data);
        Assert.Equal("invite1", result.Data.Id);

        _matchInviteRepositoryMock.Verify(r => r.Add(It.IsAny<MatchInvite>(), It.IsAny<ApplicationDbContext>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversationServiceMock.Verify(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvite_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join my match!"
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("SenderId", "Sender ID is required")
        });

        _createInviteValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender ID is required", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join my match!"
        };

        var sender = new ApplicationUser { Id = "sender1", UserName = "sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };
        var match = new MatchBy.Models.Match { Id = "match1", Sport = Sports.Football, CreatorId = "sender1", Participants = new List<ApplicationUser>() };

        var existingInvite = new MatchInvite
        {
            Id = "existing1",
            MatchId = "match1",
            ReceiverId = "receiver1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("A pending invite already exists for this user and match", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithReceiverAlreadyParticipant_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join my match!"
        };

        var sender = new ApplicationUser { Id = "sender1", UserName = "sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            CreatorId = "sender1",
            Participants = new List<ApplicationUser> { receiver },
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User receiver1 is already a participant in this match", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithNonExistentSender_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "nonexistent",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join my match!"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender with id nonexistent not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithNonExistentReceiver_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "nonexistent",
            MatchId = "match1",
            Content = "Join my match!"
        };

        var sender = new ApplicationUser { Id = "sender1", UserName = "sender" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Receiver with id nonexistent not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithNonExistentMatch_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "nonexistent",
            Content = "Join my match!"
        };

        var sender = new ApplicationUser { Id = "sender1", UserName = "sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchBy.Models.Match?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Match with id nonexistent not found", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteInvite Tests

    [Fact]
    public async Task DeleteInvite_WithValidInvite_ShouldDeleteInvite()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "sender1";

        var invite = new MatchInvite
        {
            Id = inviteId,
            SenderId = userId,
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<bool> result = await _matchesInvitesService.DeleteInvite(inviteId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);

        _matchInviteRepositoryMock.Verify(r => r.Remove(invite, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInvite_WithWrongSender_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "wronguser";

        var invite = new MatchInvite
        {
            Id = inviteId,
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<bool> result = await _matchesInvitesService.DeleteInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the sender can delete the invite", result.ErrorMessages[0]);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidPendingInvite_ShouldAcceptInviteAndAddUserToMatch()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "receiver",
            DisplayName = "Receiver",
            Rating = 5
        };

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            Participants = new List<ApplicationUser>(),
            MaxPlayers = 10,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            Match = match,
            Receiver = user,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, invite.Status);
        Assert.Contains(user, match.Participants);
    }

    [Fact]
    public async Task AcceptInvite_WithExpiredInvite_ShouldMarkAsExpired()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The invite has expired", result.ErrorMessages[0]);
        Assert.Equal(InviteStatus.Expired, invite.Status);
    }

    [Fact]
    public async Task AcceptInvite_WithFullMatch_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var user = new ApplicationUser { Id = userId, UserName = "receiver", Rating = 5 };

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            Participants = new List<ApplicationUser>(), // Will be full
            MaxPlayers = 0, // Already full
            MinimumPlayersRating = MinimumPlayersAverage.All,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            Match = match,
            Receiver = user,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The match is already full", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithWrongReceiver_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "wronguser";

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the receiver can accept the invite", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithUserRatingBelowMinimum_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "receiver",
            DisplayName = "Receiver",
            Rating = 2 // Below minimum requirement
        };

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            Participants = new List<ApplicationUser>(),
            MaxPlayers = 10,
            MinimumPlayersRating = MinimumPlayersAverage.FiveStars, // Requires 5 stars
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            Match = match,
            Receiver = user,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("does not meet the minimum players average rating requirement to join the match", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithNonPendingStatus_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Accepted, // Already accepted
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only pending invites can be accepted", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var match = new MatchBy.Models.Match
        {
            Id = "match1",
            Sport = Sports.Football,
            Participants = new List<ApplicationUser>(),
            MaxPlayers = 10,
            MinimumPlayersRating = MinimumPlayersAverage.All,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new MatchInvite
        {
            Id = inviteId,
            MatchId = "match1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            Match = match,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains($"User with id {userId} not found", result.ErrorMessages[0]);
    }

    #endregion
}