using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.MatchInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.MatchInvites;

public class MatchesInvitesServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IMatchInviteRepository> _matchInviteRepositoryMock;
    private readonly Mock<IValidator<CreateMatchInviteDto>> _createValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesInvitesService _matchesInvitesService;

    public MatchesInvitesServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _matchInviteRepositoryMock = new Mock<IMatchInviteRepository>();
        _createValidatorMock = new Mock<IValidator<CreateMatchInviteDto>>();
        var notificationServiceMock = new Mock<INotificationService>();
        var dbContextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();

        // Setup in-memory database (only needed for SaveChanges operations)
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
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _matchesInvitesService = new MatchesInvitesService(
            _userRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _matchInviteRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createValidatorMock.Object,
            notificationServiceMock.Object);
    }


    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetInviteById Tests

    [Fact]
    public async Task GetInviteById_WithValidId_ShouldReturnInvite()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetInviteById("invite1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("invite1", result.Data.Id);
    }

    [Fact]
    public async Task GetInviteById_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchInvite?)null);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetInviteById("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region GetReceivedInvites Tests

    [Fact]
    public async Task GetReceivedInvites_WithValidUserId_ShouldReturnInvites()
    {
        // Arrange
        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", SenderId = "sender1", ReceiverId = "user1", MatchId = "match1", Status = InviteStatus.Pending, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<MatchInvite>>
        {
            Data = invites,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetReceivedInvites("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<MatchInviteDto>>> result = await _matchesInvitesService.GetReceivedInvites("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
    }

    #endregion

    #region GetMatchInvite Tests

    [Fact]
    public async Task GetMatchInvite_WithValidMatchAndReceiver_ShouldReturnInvite()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetMatchInvite("match1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.MatchId);
        Assert.Equal("receiver1", result.Data.ReceiverId);
    }

    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_WithValidData_ShouldCreateInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
        };

        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!"
        };

        var createdInvite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
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

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Setup GetMatchInvite to return null (no existing invite)
        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MatchInvite?)null);

        _matchInviteRepositoryMock
            .Setup(r => r.Add(It.IsAny<MatchInvite>(), It.IsAny<ApplicationDbContext>()))
            .Callback<MatchInvite, ApplicationDbContext>((i, db) =>
            {
                i.Id = "invite1";
            });

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInvite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.MatchId);
        _matchInviteRepositoryMock.Verify(r => r.Add(It.IsAny<MatchInvite>(), It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
        };

        var existingInvite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!"
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

        // Setup GetMatchInvite to return existing pending invite
        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingInvite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
        _matchInviteRepositoryMock.Verify(r => r.Add(It.IsAny<MatchInvite>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidInvite_ShouldAcceptInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true, Rating = 5 };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true, Rating = 5 };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender },
            MinimumPlayersRating = MinimumPlayersAverage.All
        };

        var invite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            Match = match,
            Receiver = receiver
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, invite.Status);
    }

    [Fact]
    public async Task AcceptInvite_WithFullMatch_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2", EmailConfirmed = true };
        
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            MinPlayers = 2,
            MaxPlayers = 2,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, user2 }
        };

        var invite = new MatchInvite
        {
            Id = "invite1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow,
            Match = match,
            Receiver = receiver
        };

        _matchInviteRepositoryMock
            .Setup(r => r.GetByIdAsync("invite1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invite);

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already full", result.ErrorMessages[0]);
    }

    #endregion
}


