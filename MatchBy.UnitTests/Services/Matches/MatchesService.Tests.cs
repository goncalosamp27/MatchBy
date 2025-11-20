using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Matches;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.Matches;

public class MatchesServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreateMatchDto>> _createValidatorMock;
    private readonly Mock<IValidator<UpdateMatchDto>> _updateValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchesService _matchesService;

    public MatchesServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreateMatchDto>>();
        _updateValidatorMock = new Mock<IValidator<UpdateMatchDto>>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _matchesService = new MatchesService(
            _dbContext,
            _createValidatorMock.Object,
            _updateValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetMatches Tests

    [Fact]
    public async Task GetMatches_WithPublicMatches_ShouldReturnMatches()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Status = MatchStatus.Pendent
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatches(null, null, null, 1, 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data.Data);
    }

    [Fact]
    public async Task GetMatches_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var match1 = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match 1",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Status = MatchStatus.Pendent
        };

        var match2 = new Match
        {
            Id = "match2",
            CreatorId = "user1",
            Description = "Test Match 2",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Status = MatchStatus.Confirmed
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Matches.AddRangeAsync(match1, match2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PaginationResponse<List<MatchDto>>> result = await _matchesService.GetMatches(MatchStatus.Pendent, null, null, 1, 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data!.Data);
        Assert.Equal("match1", result.Data.Data[0].Id);
    }

    #endregion

    #region GetMatchById Tests

    [Fact]
    public async Task GetMatchById_WithValidId_ShouldReturnMatch()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById("match1", null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("match1", result.Data.Id);
    }

    [Fact]
    public async Task GetMatchById_WithInvalidId_ShouldReturnFailure()
    {
        // Act
        Result<MatchDto> result = await _matchesService.GetMatchById("nonexistent", null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region CreateMatch Tests

    [Fact]
    public async Task CreateMatch_WithValidData_ShouldCreateMatch()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreateMatchDto
        {
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0,
                0,
                "City",
                "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            Sport = Sports.Basketball,
            MinPlayers = 2,
            MaxPlayers = 10,
            Privacy = MatchPrivacy.Private
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMatchDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<bool> result = await _matchesService.CreateMatch(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        
        Match? createdMatch = await _dbContext.Matches.FirstOrDefaultAsync(m => m.CreatorId == "user1");
        Assert.NotNull(createdMatch);
        Assert.Equal("Test Match", createdMatch.Description);
    }

    #endregion

    #region JoinMatch Tests

    [Fact]
    public async Task JoinMatch_WithValidMatch_ShouldJoinMatch()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var participant = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Participants = new List<ApplicationUser> { creator }
        };

        await _dbContext.Users.AddRangeAsync(creator, participant);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch("match1", "user2");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        Match? updatedMatch = await _dbContext.Matches.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == "match1");
        Assert.Contains(updatedMatch!.Participants, p => p.Id == "user2");
    }

    [Fact]
    public async Task JoinMatch_WithFullMatch_ShouldReturnFailure()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var participant = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 2,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Participants = new List<ApplicationUser> { creator, participant }
        };

        await _dbContext.Users.AddRangeAsync(creator, participant);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<MatchDto> result = await _matchesService.JoinMatch("match1", "user3");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("too many participants", result.ErrorMessages[0]);
    }

    #endregion

    #region LeaveMatch Tests

    [Fact]
    public async Task LeaveMatch_WithParticipant_ShouldRemoveParticipant()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var participant = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Participants = new List<ApplicationUser> { creator, participant }
        };

        await _dbContext.Users.AddRangeAsync(creator, participant);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _matchesService.LeaveMatch("match1", "user2");

        // Assert
        Assert.True(result.Success);
        
        Match? updatedMatch = await _dbContext.Matches.Include(m => m.Participants).FirstOrDefaultAsync(m => m.Id == "match1");
        Assert.DoesNotContain(updatedMatch!.Participants, p => p.Id == "user2");
    }

    [Fact]
    public async Task LeaveMatch_WithCreator_ShouldSoftDeleteMatch()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var match = new Match
        {
            Id = "match1",
            CreatorId = "user1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow,
            minPlayers = 2,
            maxPlayers = 10,
            Sport = Sports.Basketball,
            Privacy = MatchPrivacy.Public,
            Participants = new List<ApplicationUser> { creator }
        };

        await _dbContext.Users.AddAsync(creator);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<bool> result = await _matchesService.LeaveMatch("match1", "user1");

        // Assert
        Assert.True(result.Success);
        
        Match? deletedMatch = await _dbContext.Matches.FindAsync("match1");
        Assert.NotNull(deletedMatch!.DeletedAtUtc);
    }

    #endregion
}


