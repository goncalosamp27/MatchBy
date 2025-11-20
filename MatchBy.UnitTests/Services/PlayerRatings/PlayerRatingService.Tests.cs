using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.PlayerRatings;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.PlayerRatings;

public class PlayerRatingServiceTests : IDisposable
{
    private readonly Mock<IValidator<CreatePlayerRatingDto>> _createValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly PlayerRatingService _playerRatingService;

    public PlayerRatingServiceTests()
    {
        _createValidatorMock = new Mock<IValidator<CreatePlayerRatingDto>>();
        var updateValidatorMock = new Mock<IValidator<UpdatePlayerRatingDto>>();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _playerRatingService = new PlayerRatingService(
            _dbContext,
            _createValidatorMock.Object,
            updateValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetRatingById Tests

    [Fact]
    public async Task GetRatingById_WithValidId_ShouldReturnRating()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
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
            Sport = Sports.Basketball
        };

        var rating = new PlayerRating
        {
            Id = "rating1",
            SentById = "user1",
            ReceivedById = "user2",
            MatchId = "match1",
            Rating = 4.5f,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.GetRatingById("rating1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("rating1", result.Data.Id);
        Assert.Equal(4.5f, result.Data.Rating);
    }

    #endregion

    #region CreateRating Tests

    [Fact]
    public async Task CreateRating_WithValidData_ShouldCreateRating()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
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
            Participants = new List<ApplicationUser> { sender, receiver }
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreatePlayerRatingDto
        {
            SentById = "user1",
            ReceivedById = "user2",
            MatchId = "match1",
            Rating = 4.5f
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePlayerRatingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRating(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(4.5f, result.Data.Rating);
    }

    [Fact]
    public async Task CreateRating_WithExistingRating_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        var receiver = new ApplicationUser { Id = "user2", UserName = "user2", Email = "user2@test.com", DisplayName = "User 2" };
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
            Participants = new List<ApplicationUser> { sender, receiver }
        };

        var existingRating = new PlayerRating
        {
            Id = "rating1",
            SentById = "user1",
            ReceivedById = "user2",
            MatchId = "match1",
            Rating = 4.0f,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.PlayerRatings.AddAsync(existingRating);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreatePlayerRatingDto
        {
            SentById = "user1",
            ReceivedById = "user2",
            MatchId = "match1",
            Rating = 4.5f
        };

        _createValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePlayerRatingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRating(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessages[0]);
    }

    #endregion

    #region GetAverageRatingForUser Tests

    [Fact]
    public async Task GetAverageRatingForUser_WithRatings_ShouldReturnAverage()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        
        var rating1 = new PlayerRating
        {
            Id = "rating1",
            SentById = "user2",
            ReceivedById = "user1",
            MatchId = "match1",
            Rating = 4.0f,
            CreatedAtUtc = DateTime.UtcNow
        };

        var rating2 = new PlayerRating
        {
            Id = "rating2",
            SentById = "user3",
            ReceivedById = "user1",
            MatchId = "match2",
            Rating = 5.0f,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.PlayerRatings.AddRangeAsync(rating1, rating2);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<double> result = await _playerRatingService.GetAverageRatingForUser("user1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4.5, result.Data);
    }

    [Fact]
    public async Task GetAverageRatingForUser_WithNoRatings_ShouldReturnZero()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user1", UserName = "user1", Email = "user1@test.com", DisplayName = "User 1" };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        Result<double> result = await _playerRatingService.GetAverageRatingForUser("user1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0.0, result.Data);
    }

    #endregion
}



