using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.MatchInvite;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.MatchInvites;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.MatchInvites;

public class MatchesInvitesServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateMatchInviteDto>> _createValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesInvitesService _matchesInvitesService;

    public MatchesInvitesServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateMatchInviteDto>>();
        var updateValidatorMock = new Mock<IValidator<UpdateMatchInviteDto>>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _matchesInvitesService = new MatchesInvitesService(
            _dbContext,
            _createValidatorMock.Object,
            updateValidatorMock.Object);
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
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.GetInviteById("invite1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("invite1", result.Data.Id);
    }

    #endregion

    #region CreateInvite Tests

    [Fact]
    public async Task CreateInvite_WithValidData_ShouldCreateInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.MatchId);
    }

    [Fact]
    public async Task CreateInvite_WithExistingPendingInvite_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
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

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.MatchInvites.AddAsync(existingInvite);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateMatchInviteDto
        {
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Content = "Join us!"
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchInviteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.CreateInvite(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public async Task AcceptInvite_WithValidInvite_ShouldAcceptInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Accepted, result.Data!.Status);
        
        Match? updatedMatch = await _dbContext.Matches.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == "match1");
        Assert.Contains(updatedMatch!.Participants, p => p.Id == "receiver1");
    }

    [Fact]
    public async Task AcceptInvite_WithFullMatch_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 2,
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver, user2);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.AcceptInvite("invite1", "receiver1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already full", result.ErrorMessages[0]);
    }

    #endregion

    #region DeclineInvite Tests

    [Fact]
    public async Task DeclineInvite_WithValidInvite_ShouldDeclineInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender", Email = "sender@test.com", DisplayName = "Sender" };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver", Email = "receiver@test.com", DisplayName = "Receiver" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "sender1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender }
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchInviteDto> result = await _matchesInvitesService.DeclineInvite("invite1", "receiver1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(InviteStatus.Declined, result.Data!.Status);
        Assert.NotNull(result.Data.DeclinedAtUtc);
    }

    #endregion
}


