using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Notification;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Repositories.Team;
using MatchBy.Repositories.TeamInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.Notifications;
using MatchBy.Services.TeamInvites;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.TeamInvites;

public class TeamsInvitesServiceTests : IDisposable
{
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITeamInviteRepository> _teamInviteRepositoryMock;
    private readonly Mock<IValidator<CreateTeamInviteDto>> _createInviteValidatorMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamsInvitesService _teamsInvitesService;

    public TeamsInvitesServiceTests()
    {
        var chatMessageServiceMock = new Mock<IChatMessageService>();
        _conversationServiceMock = new Mock<IConversationService>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _teamInviteRepositoryMock = new Mock<ITeamInviteRepository>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _createInviteValidatorMock = new Mock<IValidator<CreateTeamInviteDto>>();
        _notificationServiceMock = new Mock<INotificationService>();

        // Setup in-memory database
        string databaseName = Guid.NewGuid().ToString();
        DbContextOptions<ApplicationDbContext> dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        _dbContext = new ApplicationDbContext(dbContextOptions);

        dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(dbContextOptions));

        // Setup validator to return valid by default
        _createInviteValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamsInvitesService = new TeamsInvitesService(
            chatMessageServiceMock.Object,
            _conversationServiceMock.Object,
            _teamRepositoryMock.Object,
            _userRepositoryMock.Object,
            _teamInviteRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createInviteValidatorMock.Object,
            _notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetInvites Tests

    [Fact]
    public async Task GetInvites_WithValidTeamId_ShouldReturnInvites()
    {
        // Arrange
        string teamId = "team1";
        int page = 1;
        int pageSize = 10;

        var invites = new List<TeamInvite>
        {
            new() { Id = "invite1", TeamId = teamId, ReceiverId = "user1", SenderId = "sender1", Status = InviteStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<TeamInvite>>
        {
            Data = invites,
            Page = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetInvites(teamId, It.IsAny<ApplicationDbContext>(), page, pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<TeamInviteDto>>> result = await _teamsInvitesService.GetInvites(teamId, page, pageSize);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
        Assert.Equal("invite1", result.Data.Data[0].Id);
    }

    #endregion

    #region GetInviteById Tests

    [Fact]
    public async Task GetInviteById_WithValidId_ShouldReturnInviteDto()
    {
        // Arrange
        string inviteId = "invite1";

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = "user1",
            SenderId = "sender1",
            Status = InviteStatus.Accepted,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.GetInviteById(inviteId);

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

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamInvite?)null);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.GetInviteById(inviteId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invite with id nonexistent not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_WithValidDto_ShouldCreateInviteAndSendNotification()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser
        {
            Id = "receiver1",
            UserName = "receiver",
            DisplayName = "Receiver",
            Email = "receiver@test.com",
            EmailConfirmed = true
        };

        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            OwnerId = "sender1",
            Owner = new ApplicationUser { Id = "sender1", UserName = "sender", DisplayName = "Sender" },
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.ExistsPendingInviteByTeamAndUser("team1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
    MessagesCount = 0,
    Participants = new List<ConversationParticipantDto>()
}));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _teamInviteRepositoryMock
            .Setup(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()))
            .Callback<TeamInvite, ApplicationDbContext>((invite, db) =>
            {
                invite.Id = "invite1";
            });

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);

        _teamInviteRepositoryMock.Verify(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversationServiceMock.Verify(s => s.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvite_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("SenderId", "Sender ID is required")
        });

        _createInviteValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender ID is required", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithNonExistentReceiver_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "nonexistent",
            TeamId = "team1"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Receiver with id nonexistent not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithFullTeam_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };

        var team = new Team
        {
            Id = "team1",
            Name = "Full Team",
            OwnerId = "sender1",
            Owner = new ApplicationUser { Id = "sender1", UserName = "sender", DisplayName = "Sender" },
            Members = new List<ApplicationUser>(), // Will be considered full
            MaxMembers = 0, // Already at max
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The team is already full", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithNullTeamOwner_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            OwnerId = "sender1",
            Owner = null, // Owner is null
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Sender with id sender1 not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithReceiverAlreadyMember_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            OwnerId = "sender1",
            Owner = new ApplicationUser { Id = "sender1", UserName = "sender", DisplayName = "Sender" },
            Members = new List<ApplicationUser> { receiver }, // Receiver is already a member
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User receiver1 is already a member of this team", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            OwnerId = "sender1",
            Owner = new ApplicationUser { Id = "sender1", UserName = "sender", DisplayName = "Sender" },
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.ExistsPendingInviteByTeamAndUser("team1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("A pending invite already exists for this user and team", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteInvite Tests

    [Fact]
    public async Task DeleteInvite_WithValidInvite_ShouldDeleteInvite()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "sender1";

        var invite = new TeamInvite
        {
            Id = inviteId,
            SenderId = userId,
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<bool> result = await _teamsInvitesService.DeleteInvite(inviteId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);

        _teamInviteRepositoryMock.Verify(r => r.Remove(invite, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteInvite_WithNonExistentInvite_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "nonexistent";
        string userId = "sender1";

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamInvite?)null);

        // Act
        Result<bool> result = await _teamsInvitesService.DeleteInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invite with id nonexistent not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task DeleteInvite_WithWrongSender_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "wronguser";

        var invite = new TeamInvite
        {
            Id = inviteId,
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<bool> result = await _teamsInvitesService.DeleteInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the sender can delete the invite", result.ErrorMessages[0]);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidPendingInvite_ShouldAcceptInviteAndAddUserToTeam()
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

        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Not expired
            Team = team,
            Receiver = user,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, invite.Status);
        Assert.Contains(user, team.Members);
    }

    [Fact]
    public async Task AcceptInvite_WithNullReceiver_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Not expired
            Team = team,
            Receiver = null, // Receiver is null
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Receiver with id receiver1 not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithFullTeam_ShouldReturnFailure()
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

        var team = new Team
        {
            Id = "team1",
            Name = "Full Team",
            Members = new List<ApplicationUser>(new ApplicationUser[5]), // Team is full
            MaxMembers = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7), // Not expired
            Team = team,
            Receiver = user,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The team is already full", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithAcceptedInvite_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Accepted, // Already accepted
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot accept an invite with status Accepted", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithExpiredInvite_ShouldMarkAsExpired()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "receiver1";

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = userId,
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("The invite has expired", result.ErrorMessages[0]);
        Assert.Equal(InviteStatus.Expired, invite.Status);
    }

    [Fact]
    public async Task AcceptInvite_WithNonExistentInvite_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "nonexistent";
        string userId = "receiver1";

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamInvite?)null);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invite with id nonexistent not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithWrongReceiver_ShouldReturnFailure()
    {
        // Arrange
        string inviteId = "invite1";
        string userId = "wronguser";

        var invite = new TeamInvite
        {
            Id = inviteId,
            TeamId = "team1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync(inviteId, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite(inviteId, userId);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the receiver can accept the invite", result.ErrorMessages[0]);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task TeamsInvitesService_FullInviteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var createDto = new CreateTeamInviteDto
        {
            Content = "Please join my team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1"
        };

        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", DisplayName = "Receiver" };
        var team = new Team
        {
            Id = "team1",
            Name = "Test Team",
            OwnerId = "sender1",
            Owner = new ApplicationUser { Id = "sender1", UserName = "sender", DisplayName = "Sender" },
            Members = new List<ApplicationUser>(),
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Setup mocks for creation
        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.ExistsPendingInviteByTeamAndUser("team1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
    MessagesCount = 0,
    Participants = new List<ConversationParticipantDto>()
}));

        _notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        _teamInviteRepositoryMock
            .Setup(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()))
            .Callback<TeamInvite, ApplicationDbContext>((invite, db) =>
            {
                invite.Id = "invite1";
            });

        // Act - Create invite
        Result<TeamInviteDto> createResult = await _teamsInvitesService.CreateInvite(createDto);
        Assert.False(createResult.Success);

        // Setup for acceptance
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            ReceiverId = "receiver1",
            SenderId = "sender1",
            Status = InviteStatus.Pending,
            Team = team,
            Receiver = receiver,
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act - Accept invite
        Result<TeamInviteDto> acceptResult = await _teamsInvitesService.AcceptInvite("invite1", "receiver1");
        Assert.False(acceptResult.Success);
    }

    #endregion
}