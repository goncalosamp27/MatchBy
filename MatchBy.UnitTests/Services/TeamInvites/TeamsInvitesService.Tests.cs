using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Services.Notifications;
using MatchBy.Services.TeamInvites;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace MatchBy.UnitTests.Services.TeamInvites;

public class TeamsInvitesServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateTeamInviteDto>> _createValidatorMock;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _dbContextFactoryMock;
    private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamsInvitesService _teamsInvitesService;

    public TeamsInvitesServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateTeamInviteDto>>();
        var updateValidatorMock = new Mock<IValidator<UpdateTeamInviteDto>>();
        var notificationServiceMock = new Mock<INotificationService>();
        _dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database with a unique name per test class
        // All contexts created with these options will share the same database
        string databaseName = Guid.NewGuid().ToString();
        _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        // Create a test context for setup and verification
        // This context will share the same in-memory database as contexts created by the factory
        _dbContext = new ApplicationDbContext(_dbContextOptions);

        // Setup the factory to return a new instance each time
        // This prevents the service from disposing the test's context instance
        _dbContextFactoryMock
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(_dbContextOptions));

        _teamsInvitesService = new TeamsInvitesService(
            _dbContextFactoryMock.Object,
            _createValidatorMock.Object,
            updateValidatorMock.Object,
            notificationServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Helper method to create a fresh DbContext for verification.
    /// This ensures we're querying the database directly rather than using cached entities from the change tracker.
    /// </summary>
    private ApplicationDbContext CreateFreshDbContext()
    {
        return new ApplicationDbContext(_dbContextOptions);
    }

    #region GetInviteById Tests

    [Fact]
    public async Task GetInviteById_WithValidId_ShouldReturnInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team", 
            Description = "Test Description",
            OwnerId = "sender1", 
            MaxMembers = 10,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

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
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("team1", result.Data.TeamId);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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

        var existingInvite = new TeamInvite
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

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(existingInvite);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateInvite_WithFullTeam_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var team = new Team 
        { 
            Id = "team1", 
            Name = "Test Team",
            Description = "Test Description",
            OwnerId = "sender1", 
            MaxMembers = 1,
            Privacy = TeamPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { sender }
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateTeamInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Content = "Join us!"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateTeamInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already full", result.ErrorMessages[0]);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidInvite_ShouldAcceptInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, result.Data!.Status);
        
        // Use a fresh context to verify the changes were persisted
        await using ApplicationDbContext freshContext = CreateFreshDbContext();
        Team? updatedTeam = await freshContext.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == "team1");
        Assert.NotNull(updatedTeam);
        Assert.Contains(updatedTeam.Members, m => m.Id == "receiver1");
    }

    [Fact]
    public async Task AcceptInvite_WithNonReceiver_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

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
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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
            CreatedAtUtc = DateTime.UtcNow.AddDays(-7)
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("expired", result.ErrorMessages[0]);
    }

    #endregion

    #region DeclineInvite Tests

    [Fact]
    public async Task DeclineInvite_WithValidInvite_ShouldDeclineInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.DeclineInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Declined, result.Data!.Status);
        Assert.NotNull(result.Data.DeclinedAtUtc);
    }

    #endregion

    #region CancelInvite Tests

    [Fact]
    public async Task CancelInvite_WithValidInvite_ShouldCancelInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Teams.AddAsync(team);
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CancelInvite("invite1", "sender1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Cancelled, result.Data!.Status);
    }

    [Fact]
    public async Task CancelInvite_WithNonSender_ShouldReturnFailure()
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

        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<TeamInviteDto> result = await _teamsInvitesService.CancelInvite("invite1", "user2");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the sender", result.ErrorMessages[0]);
    }

    #endregion
}


