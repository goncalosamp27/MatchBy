using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Notification;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Repositories.Team;
using MatchBy.Repositories.TeamInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.Notifications;
using MatchBy.Services.TeamInvites;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.TeamInvites;

public class TeamsInvitesServiceTests : IDisposable
{
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITeamInviteRepository> _teamInviteRepositoryMock;
    private readonly Mock<IValidator<CreateTeamInviteDto>> _createValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamsInvitesService _teamsInvitesService;

    public TeamsInvitesServiceTests()
    {
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _teamInviteRepositoryMock = new Mock<ITeamInviteRepository>();
        _createValidatorMock = new Mock<IValidator<CreateTeamInviteDto>>();
        var notificationServiceMock = new Mock<INotificationService>();
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

        // Setup notification service
        notificationServiceMock
            .Setup(s => s.SendNotificationAsync(It.IsAny<CreateNotificationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Setup validator to return valid by default
        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _teamsInvitesService = new TeamsInvitesService(
            _teamRepositoryMock.Object,
            _userRepositoryMock.Object,
            _teamInviteRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createValidatorMock.Object,
            notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region GetInviteById Tests

    [Fact]
    public async Task GetInviteById_WithValidId_ShouldReturnInvite()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.GetInviteById("invite1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("invite1", result.Data.Id);
    }

    [Fact]
    public async Task GetInviteById_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TeamInvite?)null);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.GetInviteById("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }
    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_WithValidData_ShouldCreateInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1",
            Owner = sender,
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        var createdInvite = new TeamInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.ExistsPendingInviteByTeamAndUser("team1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _teamInviteRepositoryMock
            .Setup(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()))
            .Callback<TeamInvite, ApplicationDbContext>((i, db) =>
            {
                i.Id = "invite1";
            });

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInvite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("team1", result.Data.TeamId);
        _teamInviteRepositoryMock.Verify(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1",
            Owner = sender,
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

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
        Assert.Contains("already exists", result.ErrorMessages[0]);
        _teamInviteRepositoryMock.Verify(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    [Fact]
    public async Task CreateInvite_WithFullTeam_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1",
            Owner = sender,
            MaxMembers = 1,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sender);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _teamRepositoryMock
            .Setup(r => r.GetTeamUserOwnsByIdAsync("team1", "sender1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        _teamInviteRepositoryMock
            .Setup(r => r.ExistsPendingInviteByTeamAndUser("team1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already full", result.ErrorMessages[0]);
        _teamInviteRepositoryMock.Verify(r => r.Add(It.IsAny<TeamInvite>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidInvite_ShouldAcceptInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1", 
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        var invite = new TeamInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            Team = team,
            Receiver = receiver
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, invite.Status);
        Assert.Contains(team.Members, m => m.Id == "receiver1");
    }

    [Fact]
    public async Task AcceptInvite_WithNonReceiver_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1", 
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };
        
        var invite = new TeamInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            Team = team,
            Receiver = receiver
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite("invite1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the receiver can accept", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task AcceptInvite_WithExpiredInvite_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1", 
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        var invite = new TeamInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
            Team = team,
            Receiver = receiver
        };

        _teamInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("expired", result.ErrorMessages[0]);
    }

    #endregion
    
}


