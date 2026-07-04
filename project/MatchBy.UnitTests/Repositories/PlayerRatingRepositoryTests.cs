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
    public async Task GetRatingsForMatch_WithExistingRatings_ShouldReturnPagedRatings()
    {
        // Arrange
        var sentBy = new ApplicationUser { Id = "sentBy1", UserName = "sentBy1", DisplayName = "Sent By", Email = "sentby@test.com", EmailConfirmed = true };
        var receivedBy = new ApplicationUser { Id = "receivedBy1", UserName = "receivedBy1", DisplayName = "Received By", Email = "receivedby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sentBy, receivedBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", Rating = 5, SentById = "sentBy1", ReceivedById = "receivedBy1", MatchId = "match1", Comment = "Great player!", CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "rating2", Rating = 4, SentById = "sentBy1", ReceivedById = "receivedBy1", MatchId = "match1", Comment = "Good player!", CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsForMatch("match1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetRatingsForMatch_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var sentBy = new ApplicationUser { Id = "sentBy1", UserName = "sentBy1", DisplayName = "Sent By", Email = "sentby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sentBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>();
        for (int i = 1; i <= 5; i++)
        {
            var receivedBy = new ApplicationUser { Id = $"receivedBy{i}", UserName = $"receivedBy{i}", DisplayName = $"Received By {i}", Email = $"receivedby{i}@test.com", EmailConfirmed = true };
            await _dbContext.Users.AddAsync(receivedBy);

            ratings.Add(new PlayerRating
            {
                Id = $"rating{i}",
                Rating = 5,
                SentById = "sentBy1",
                ReceivedById = $"receivedBy{i}",
                MatchId = "match1",
                Comment = $"Comment {i}",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsForMatch("match1", _dbContext, page: 2, pageSize: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetRatingsForMatch_WithNoRatings_ShouldReturnEmptyList()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsForMatch("match1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
    }

    #endregion

    #region GetRatingsGivenByUser Tests

    [Fact]
    public async Task GetRatingsGivenByUser_WithExistingRatings_ShouldReturnPagedRatings()
    {
        // Arrange
        var sentBy = new ApplicationUser { Id = "sentBy1", UserName = "sentBy1", DisplayName = "Sent By", Email = "sentby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sentBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", Rating = 5, SentById = "sentBy1", ReceivedById = "receivedBy1", MatchId = "match1", Comment = "Great player!", CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "rating2", Rating = 4, SentById = "sentBy1", ReceivedById = "receivedBy2", MatchId = "match1", Comment = "Good player!", CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsGivenByUser("sentBy1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    #endregion

    #region GetRatingsReceivedByUser Tests

    [Fact]
    public async Task GetRatingsReceivedByUser_WithExistingRatings_ShouldReturnPagedRatings()
    {
        // Arrange
        var receivedBy = new ApplicationUser { Id = "receivedBy1", UserName = "receivedBy1", DisplayName = "Received By", Email = "receivedby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(receivedBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var ratings = new List<PlayerRating>
        {
            new() { Id = "rating1", Rating = 5, SentById = "sentBy1", ReceivedById = "receivedBy1", MatchId = "match1", Comment = "Great player!", CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "rating2", Rating = 4, SentById = "sentBy2", ReceivedById = "receivedBy1", MatchId = "match1", Comment = "Good player!", CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.PlayerRatings.AddRangeAsync(ratings);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<PlayerRating>> result = await _repository.GetRatingsReceivedByUser("receivedBy1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnPlayerRating()
    {
        // Arrange
        var sentBy = new ApplicationUser { Id = "sentBy1", UserName = "sentBy1", DisplayName = "Sent By", Email = "sentby@test.com", EmailConfirmed = true };
        var receivedBy = new ApplicationUser { Id = "receivedBy1", UserName = "receivedBy1", DisplayName = "Received By", Email = "receivedby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sentBy, receivedBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var rating = new PlayerRating
        {
            Id = "rating1",
            Rating = 5,
            SentById = "sentBy1",
            ReceivedById = "receivedBy1",
            MatchId = "match1",
            Comment = "Great player!",
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PlayerRating? result = await _repository.GetByIdAsync("rating1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("rating1", result.Id);
        Assert.Equal(5, result.Rating);
        Assert.Equal("sentBy1", result.SentById);
        Assert.Equal("receivedBy1", result.ReceivedById);
        Assert.Equal("match1", result.MatchId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        PlayerRating? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithCompositeKey_ExistingRating_ShouldReturnPlayerRating()
    {
        // Arrange
        var sentBy = new ApplicationUser { Id = "sentBy1", UserName = "sentBy1", DisplayName = "Sent By", Email = "sentby@test.com", EmailConfirmed = true };
        var receivedBy = new ApplicationUser { Id = "receivedBy1", UserName = "receivedBy1", DisplayName = "Received By", Email = "receivedby@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sentBy, receivedBy, creator);
        await _dbContext.SaveChangesAsync();

        var match = new Match
        {
            Id = "match1",
            Address = "Test Address",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Description = "Test Match",
            MinPlayers = 2,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };
        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var rating = new PlayerRating
        {
            Id = "rating1",
            Rating = 5,
            SentById = "sentBy1",
            ReceivedById = "receivedBy1",
            MatchId = "match1",
            Comment = "Great player!",
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        PlayerRating? result = await _repository.GetByIdAsync("sentBy1", "receivedBy1", "match1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("rating1", result.Id);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task GetByIdAsync_WithCompositeKey_NonExistentRating_ShouldReturnNull()
    {
        // Act
        PlayerRating? result = await _repository.GetByIdAsync("sentBy1", "receivedBy1", "match1", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddPlayerRatingToContext()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            Rating = 5,
            SentById = "sentBy1",
            ReceivedById = "receivedBy1",
            MatchId = "match1",
            Comment = "Great player!",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(rating, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(rating, _dbContext.PlayerRatings);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkPlayerRatingAsModified()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            Rating = 5,
            SentById = "sentBy1",
            ReceivedById = "receivedBy1",
            MatchId = "match1",
            Comment = "Great player!",
            CreatedAtUtc = DateTime.UtcNow
        };
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
    public async Task Remove_ShouldRemovePlayerRatingFromContext()
    {
        // Arrange
        var rating = new PlayerRating
        {
            Id = "rating1",
            Rating = 5,
            SentById = "sentBy1",
            ReceivedById = "receivedBy1",
            MatchId = "match1",
            Comment = "Great player!",
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.PlayerRatings.AddAsync(rating);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(rating, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(rating, _dbContext.PlayerRatings);
    }

    #endregion
}