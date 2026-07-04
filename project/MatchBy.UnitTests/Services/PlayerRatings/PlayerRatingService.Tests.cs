using FluentValidation;
using MatchBy.Data;
using MatchBy.DTOs.PlayerRating;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.PlayerRating;
using MatchBy.Repositories.User;
using MatchBy.Services.PlayerRatings;
using Microsoft.EntityFrameworkCore;
using Moq;
using Match = MatchBy.Models.Match;

namespace MatchBy.UnitTests.Services.PlayerRatings;

public class PlayerRatingServiceTests : IDisposable
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IPlayerRatingRepository> _playerRatingRepositoryMock;
    private readonly Mock<IValidator<CreatePlayerRatingDto>> _createRatingValidatorMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly PlayerRatingService _playerRatingService;

    public PlayerRatingServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _playerRatingRepositoryMock = new Mock<IPlayerRatingRepository>();
        _createRatingValidatorMock = new Mock<IValidator<CreatePlayerRatingDto>>();
        var updateRatingValidatorMock = new Mock<IValidator<UpdatePlayerRatingDto>>();
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
        _createRatingValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<CreatePlayerRatingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        updateRatingValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdatePlayerRatingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _playerRatingService = new PlayerRatingService(
            _userRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _playerRatingRepositoryMock.Object,
            dbContextFactoryMock.Object,
            _createRatingValidatorMock.Object,
            updateRatingValidatorMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetRatingByIdAsync Tests

    [Fact]
    public async Task GetRatingByIdAsync_WithValidId_ShouldReturnRatingDto()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("rating1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.GetRatingByIdAsync("rating1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("rating1", result.Data.Id);
        Assert.Equal(5, result.Data.Rating);
    }

    [Fact]
    public async Task GetRatingByIdAsync_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerRating?)null);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.GetRatingByIdAsync("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region GetRatingsForMatchAsync Tests

    [Fact]
    public async Task GetRatingsForMatchAsync_WithValidMatchId_ShouldReturnRatings()
    {
        // Arrange
        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "rating2", MatchId = "match1", SentById = "receiver1", ReceivedById = "sender1", Rating = 4, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<PlayerRating>>
        {
            Data = ratings,
            TotalCount = 2,
            Page = 1,
            PageSize = 10
        };

        _matchRepositoryMock
            .Setup(r => r.ExistsAsync("match1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _playerRatingRepositoryMock
            .Setup(r => r.GetRatingsForMatch("match1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<PlayerRatingDto>>> result = await _playerRatingService.GetRatingsForMatchAsync("match1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Data.Count);
        Assert.Equal(2, result.Data.TotalCount);
    }

    [Fact]
    public async Task GetRatingsForMatchAsync_WithNonExistentMatch_ShouldReturnFailure()
    {
        // Arrange
        _matchRepositoryMock
            .Setup(r => r.ExistsAsync("nonexistent", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<PaginationResponse<List<PlayerRatingDto>>> result = await _playerRatingService.GetRatingsForMatchAsync("nonexistent", page: 1, pageSize: 10);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessages[0]);
    }

    #endregion

    #region GetRatingsGivenByUserAsync Tests

    [Fact]
    public async Task GetRatingsGivenByUserAsync_WithValidUserId_ShouldReturnRatings()
    {
        // Arrange
        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", MatchId = "match1", SentById = "user1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<PlayerRating>>
        {
            Data = ratings,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetRatingsGivenByUser("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<PlayerRatingDto>>> result = await _playerRatingService.GetRatingsGivenByUserAsync("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("user1", result.Data.Data[0].SentById);
    }

    #endregion

    #region GetRatingsReceivedByUserAsync Tests

    [Fact]
    public async Task GetRatingsReceivedByUserAsync_WithValidUserId_ShouldReturnRatings()
    {
        // Arrange
        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "user1", Rating = 5, CreatedAtUtc = DateTime.UtcNow }
        };

        var paginationResponse = new PaginationResponse<List<PlayerRating>>
        {
            Data = ratings,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetRatingsReceivedByUser("user1", It.IsAny<ApplicationDbContext>(), 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginationResponse);

        // Act
        Result<PaginationResponse<List<PlayerRatingDto>>> result = await _playerRatingService.GetRatingsReceivedByUserAsync("user1", page: 1, pageSize: 10);

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Data.Data);
        Assert.Equal("user1", result.Data.Data[0].ReceivedById);
    }

    #endregion

    #region CreateRatingAsync Tests

    [Fact]
    public async Task CreateRatingAsync_WithValidDto_ShouldCreateRating()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, receiver }
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var createDto = new CreatePlayerRatingDto
        {
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5,
            Comment = "Great player"
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", "receiver1", "match1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerRating?)null);

        _playerRatingRepositoryMock
            .Setup(r => r.GetRatingsReceivedByUser("receiver1", It.IsAny<ApplicationDbContext>(), 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponse<List<PlayerRating>>
            {
                Data = new List<PlayerRating>(),
                TotalCount = 0,
                Page = 1,
                PageSize = int.MaxValue
            });

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRatingAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.Rating);
    }

    [Fact]
    public async Task CreateRatingAsync_WithInvalidValidation_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreatePlayerRatingDto
        {
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 6 // Invalid rating (should be 1-5)
        };

        var validationResult = new FluentValidation.Results.ValidationResult(new[]
        {
            new FluentValidation.Results.ValidationFailure("Rating", "Rating must be between 1 and 5")
        });

        _createRatingValidatorMock
            .Setup(v => v.ValidateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRatingAsync(createDto);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateRatingAsync_WithNonExistentMatch_ShouldReturnFailure()
    {
        // Arrange
        var createDto = new CreatePlayerRatingDto
        {
            MatchId = "nonexistent",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("nonexistent", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Match?)null);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRatingAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Match not found", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateRatingAsync_WithNonParticipant_ShouldReturnFailure()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, receiver }
        };

        var createDto = new CreatePlayerRatingDto
        {
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "other1", // other1 is not a participant
            Rating = 5
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRatingAsync(createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must have participated", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task CreateRatingAsync_WithExistingRating_ShouldUpdateRating()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        var match = new Match
        {
            Id = "match1",
            CreatorId = "creator1",
            Description = "Test Match",
            Address = "Test Address",
            Location = new Location(0, 0, "City", "Country"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { sender, receiver }
        };

        var existingRating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 3,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createDto = new CreatePlayerRatingDto
        {
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5, // Updated rating
            Comment = "Updated comment"
        };

        _matchRepositoryMock
            .Setup(r => r.GetByIdAsync("match1", "sender1", false, It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("sender1", "receiver1", "match1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRating);

        _playerRatingRepositoryMock
            .Setup(r => r.GetRatingsReceivedByUser("receiver1", It.IsAny<ApplicationDbContext>(), 1, int.MaxValue, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginationResponse<List<PlayerRating>>
            {
                Data = new List<PlayerRating> { existingRating },
                TotalCount = 1,
                Page = 1,
                PageSize = int.MaxValue
            });

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync("receiver1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiver);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.CreateRatingAsync(createDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, existingRating.Rating); // Rating should be updated
    }

    #endregion

    #region UpdateRatingAsync Tests

    [Fact]
    public async Task UpdateRatingAsync_WithValidDto_ShouldUpdateRating()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 3,
            CreatedAtUtc = DateTime.UtcNow
        };

        var updateDto = new UpdatePlayerRatingDto
        {
            Id = "rating1",
            SentById = "sender1",
            Rating = 5
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("rating1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.UpdateRatingAsync(updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(5, rating.Rating);
    }

    [Fact]
    public async Task UpdateRatingAsync_WithWrongSender_ShouldReturnFailure()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 3,
            CreatedAtUtc = DateTime.UtcNow
        };

        var updateDto = new UpdatePlayerRatingDto
        {
            Id = "rating1",
            SentById = "wrong-sender",
            Rating = 5
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("rating1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);

        // Act
        Result<PlayerRatingDto> result = await _playerRatingService.UpdateRatingAsync(updateDto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the sender", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteRatingAsync Tests

    [Fact]
    public async Task DeleteRatingAsync_WithValidIdAndSender_ShouldDeleteRating()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("rating1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);

        // Act
        Result<bool> result = await _playerRatingService.DeleteRatingAsync("rating1", "sender1");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
        _playerRatingRepositoryMock.Verify(r => r.Remove(rating, It.IsAny<ApplicationDbContext>()), Times.Once);
    }

    [Fact]
    public async Task DeleteRatingAsync_WithWrongSender_ShouldReturnFailure()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            MatchId = "match1",
            SentById = "sender1",
            ReceivedById = "receiver1",
            Rating = 5,
            CreatedAtUtc = DateTime.UtcNow
        };

        _playerRatingRepositoryMock
            .Setup(r => r.GetByIdAsync("rating1", It.IsAny<ApplicationDbContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rating);

        // Act
        Result<bool> result = await _playerRatingService.DeleteRatingAsync("rating1", "wrong-sender");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the sender", result.ErrorMessages[0]);
        _playerRatingRepositoryMock.Verify(r => r.Remove(It.IsAny<PlayerRating>(), It.IsAny<ApplicationDbContext>()), Times.Never);
    }

    #endregion
}
