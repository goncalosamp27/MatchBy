using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.PlayerRating;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class PlayerRatingRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PlayerRatingRepository _repository;

    public PlayerRatingRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new PlayerRatingRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetRatingsForMatch Tests

    [Fact]
    public async Task GetRatingsForMatch_WithValidMatchId_ShouldReturnRatings()
    {
        // Arrange
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
            CreatedAtUtc = DateTime.UtcNow
        };

        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };

        await _dbContext.Users.AddRangeAsync(sender, receiver);
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "rating2", MatchId = "match1", SentById = "receiver1", ReceivedById = "sender1", Rating = 4, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsForMatch("match1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetRatingsForMatch_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>();
        for (int i = 1; i <= 10; i++)
        {
            var sender = new ApplicationUser { Id = $"sender{i}", UserName = $"sender{i}", DisplayName = $"Sender {i}", Email = $"sender{i}@test.com", EmailConfirmed = true };
            var receiver = new ApplicationUser { Id = $"receiver{i}", UserName = $"receiver{i}", DisplayName = $"Receiver {i}", Email = $"receiver{i}@test.com", EmailConfirmed = true };
            await _dbContext.Users.AddRangeAsync(sender, receiver);
            ratings.Add(new PlayerRating { Id = $"rating{i}", MatchId = "match1", SentById = $"sender{i}", ReceivedById = $"receiver{i}", Rating = 5, CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i) });
        }

        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsForMatch("match1", _dbContext, page: 2, pageSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    #endregion

    #region GetRatingsGivenByUser Tests

    [Fact]
    public async Task GetRatingsGivenByUser_WithValidUserId_ShouldReturnRatings()
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsGivenByUser("sender1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("sender1", result.Data[0].SentById);
    }

    #endregion

    #region GetRatingsReceivedByUser Tests

    [Fact]
    public async Task GetRatingsReceivedByUser_WithValidUserId_ShouldReturnRatings()
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
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsReceivedByUser("receiver1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("receiver1", result.Data[0].ReceivedById);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithRatingId_ShouldReturnRating()
    {
        // Arrange
        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PlayerRating? result = await _repository.GetByIdAsync("rating1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("rating1", result.Id);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task GetByIdAsync_WithCompositeKey_ShouldReturnRating()
    {
        // Arrange
        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PlayerRating? result = await _repository.GetByIdAsync("sender1", "receiver1", "match1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("rating1", result.Id);
        Assert.Equal("sender1", result.SentById);
        Assert.Equal("receiver1", result.ReceivedById);
        Assert.Equal("match1", result.MatchId);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddRatingToContext()
    {
        // Arrange
        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };

        // Act
        _repository.Add(rating, _dbContext);

        // Assert
        Assert.Contains(rating, _dbContext.PlayerRatings);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkRatingAsModified()
    {
        // Arrange
        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        rating.Rating = 4;

        // Act
        _repository.Update(rating, _dbContext);

        // Assert
        EntityEntry<PlayerRating> entry = _dbContext.Entry(rating);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveRatingFromContext()
    {
        // Arrange
        var rating = new PlayerRating { Id = "rating1", MatchId = "match1", SentById = "sender1", ReceivedById = "receiver1", Rating = 5, CreatedAtUtc = DateTime.UtcNow };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(rating, _dbContext);

        // Assert
        Assert.DoesNotContain(rating, _dbContext.PlayerRatings);
    }

    #endregion
}

