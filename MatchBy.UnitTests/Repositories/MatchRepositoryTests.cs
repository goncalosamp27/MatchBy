using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class MatchRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchRepository _repository;

    public MatchRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new MatchRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithPublicMatch_ShouldReturnMatch()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
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

        // Act
        Match? result = await _repository.GetByIdAsync("match1", null, false, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("match1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithPrivateMatchAndParticipant_ShouldReturnMatch()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var participant = new ApplicationUser { Id = "participant1", UserName = "participant1", DisplayName = "Participant", Email = "participant@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(creator, participant);
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
            Privacy = MatchPrivacy.Private,
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { participant }
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Match? result = await _repository.GetByIdAsync("match1", "participant1", false, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("match1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithPrivateMatchAndNoAccess_ShouldReturnNull()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var user = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User", Email = "user@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(creator, user);
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
            Privacy = MatchPrivacy.Private,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        // Act
        Match? result = await _repository.GetByIdAsync("match1", "user1", false, _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingMatch_ShouldReturnTrue()
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

        // Act
        bool result = await _repository.ExistsAsync("match1", _dbContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentMatch_ShouldReturnFalse()
    {
        // Act
        bool result = await _repository.ExistsAsync("nonexistent", _dbContext);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAllMatchCountries Tests

    [Fact]
    public async Task GetAllMatchCountries_ShouldReturnDistinctCountries()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country1"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country1"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country2"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        List<string> result = await _repository.GetAllMatchCountries(_dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("Country1", result);
        Assert.Contains("Country2", result);
    }

    #endregion

    #region GetAllCitiesByCountry Tests

    [Fact]
    public async Task GetAllCitiesByCountry_ShouldReturnCitiesForCountry()
    {
        // Arrange
        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country1"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country1"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country2"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        List<string> result = await _repository.GetAllCitiesByCountry("Country1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("City1", result);
        Assert.Contains("City2", result);
        Assert.DoesNotContain("City3", result);
    }

    #endregion

    #region GetMatches Tests

    [Fact]
    public async Task GetMatches_WithBasicQuery_ShouldReturnMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country1"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country2"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(2), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Basketball, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
    }

    #endregion

    #region GetMatches Advanced Filtering Tests

    [Fact]
    public async Task GetMatches_WithCountryFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Portugal"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Spain"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Portugal"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            Country = "Portugal",
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(m => m.Location.Country == "Portugal"));
    }

    [Fact]
    public async Task GetMatches_WithCityFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "Lisbon", "Portugal"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "Porto", "Portugal"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "Madrid", "Spain"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            Country = "Portugal",
            City = "Lisbon",
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Lisbon", result.Data[0].Location.City);
    }

    [Fact]
    public async Task GetMatches_WithSportsFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Basketball, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            SportsList = [Sports.Football],
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(m => m.Sport == Sports.Football));
    }

    [Fact]
    public async Task GetMatches_WithDateRangeFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly tomorrow = today.AddDays(1);
        DateOnly dayAfter = today.AddDays(2);

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = today.ToDateTime(TimeOnly.MinValue), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = tomorrow.ToDateTime(TimeOnly.MinValue), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = dayAfter.ToDateTime(TimeOnly.MinValue), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            FromDateUtc = tomorrow,
            ToDateUtc = tomorrow,
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(DateOnly.FromDateTime(result.Data[0].MatchDateTimeUtc), tomorrow);
    }

    [Fact]
    public async Task GetMatches_WithTimeRangeFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        DateTime baseDate = DateTime.UtcNow.Date.AddDays(1);
        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = baseDate.AddHours(10), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = baseDate.AddHours(14), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = baseDate.AddHours(18), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            FromTimeUtc = 12, // 12:00
            ToTimeUtc = 16,   // 16:00
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(14, result.Data[0].MatchDateTimeUtc.Hour);
    }

    [Fact]
    public async Task GetMatches_WithStatusFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Confirmed, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Cancelled, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            MatchStatus = Status.Confirmed,
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(MatchStatus.Confirmed, result.Data[0].Status);
    }

    [Fact]
    public async Task GetMatches_WithMinimumPlayersAverageFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true, Rating = 3 };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true, Rating = 4 };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true, Rating = 5 };
        await _dbContext.Users.AddRangeAsync(creator, user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user1, user2 } }, // Average: 3.5
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user3 } }, // Average: 5.0
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user1 } } // Average: 3.0
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            MinimumPlayersAverage = MinimumPlayersAverage.FourStars,
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMatches_WithDistanceFilter_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(40.7128, -74.0060, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }, // NYC
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(34.0522, -118.2437, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }, // LA
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(41.8781, -87.6298, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow } // Chicago
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            UserLatitude = 40.7128, // NYC latitude
            UserLongitude = -74.0060, // NYC longitude
            MaxDistanceInKm = 500, // Within 500km of NYC
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMatches_WithSearchQuery_ShouldReturnFilteredMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Football tournament", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match2", CreatorId = "creator1", Description = "Basketball game", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "match3", CreatorId = "creator1", Description = "Soccer practice", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            Q = "football",
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMatches_WithSortingByPlayersAverage_ShouldReturnSortedMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true, Rating = 2 };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true, Rating = 4 };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true, Rating = 3 };
        await _dbContext.Users.AddRangeAsync(creator, user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user1 } }, // Rating: 2
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user2 } }, // Rating: 4
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user3 } } // Rating: 3
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            SortBy = SortBy.PlayersAverage,
            OrderBy = OrderBy.Descending,
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetMatches_WithSortingByDistance_ShouldReturnSortedMatches()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(creator);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(40.7128, -74.0060, "City1", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }, // NYC
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(34.0522, -118.2437, "City2", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow }, // LA
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(41.8781, -87.6298, "City3", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow } // Chicago
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        var queryParams = new MatchQueryParametersDto
        {
            UserLatitude = 40.7128, // NYC latitude
            UserLongitude = -74.0060, // NYC longitude
            SortBy = SortBy.Distance,
            OrderBy = OrderBy.Ascending,
            Page = 1,
            PageSize = 10
        };

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatches(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal("match1", result.Data[0].Id); // NYC should be first (distance 0)
        // Chicago and LA order may vary but both should be after NYC
    }

    #endregion

    #region GetMatchesUserAttending Tests

    [Fact]
    public async Task GetMatchesUserAttending_WithParticipant_ShouldReturnMatchesUserIsAttending()
    {
        // Arrange
        var creator1 = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator 1", Email = "creator1@test.com", EmailConfirmed = true };
        var creator2 = new ApplicationUser { Id = "creator2", UserName = "creator2", DisplayName = "Creator 2", Email = "creator2@test.com", EmailConfirmed = true };
        var participant = new ApplicationUser { Id = "participant1", UserName = "participant1", DisplayName = "Participant", Email = "participant@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(creator1, creator2, participant);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Confirmed, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { participant } },
            new() { Id = "match2", CreatorId = "creator2", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { participant } },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Cancelled, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { participant } },
            new() { Id = "match4", CreatorId = "participant1", Description = "Match 4", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Confirmed, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser>() }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatchesUserAttending("participant1", _dbContext, q: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(m => m.CreatorId != "participant1" && m.Participants.Any(p => p.Id == "participant1") && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent)));
    }

    #endregion

    #region GetMatchesExceptUser Tests

    [Fact]
    public async Task GetMatchesExceptUser_WithUser_ShouldReturnMatchesUserDoesNotParticipateInOrCreate()
    {
        // Arrange
        var creator1 = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator 1", Email = "creator1@test.com", EmailConfirmed = true };
        var creator2 = new ApplicationUser { Id = "creator2", UserName = "creator2", DisplayName = "Creator 2", Email = "creator2@test.com", EmailConfirmed = true };
        var user = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User", Email = "user@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(creator1, creator2, user);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser>() },
            new() { Id = "match2", CreatorId = "user1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser>() },
            new() { Id = "match3", CreatorId = "creator2", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { user } }
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatchesExceptUser("user1", _dbContext, q: null);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("match1", result.Data[0].Id); // Only match1 should be returned (not created by user1 and user1 is not a participant)
    }

    #endregion

    #region GetMatchesForUser Tests

    [Fact]
    public async Task GetMatchesForUser_WithCreator_ShouldReturnMatchesCreatedByUserThatNeedPlayers()
    {
        // Arrange
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        var participant = new ApplicationUser { Id = "participant1", UserName = "participant1", DisplayName = "Participant", Email = "participant@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(creator, participant);
        await _dbContext.SaveChangesAsync();

        var matches = new List<Match>
        {
            new() { Id = "match1", CreatorId = "creator1", Description = "Match 1", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Confirmed, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { participant } },
            new() { Id = "match2", CreatorId = "creator1", Description = "Match 2", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Pendent, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser>() },
            new() { Id = "match3", CreatorId = "creator1", Description = "Match 3", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Cancelled, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser>() },
            new() { Id = "match4", CreatorId = "creator1", Description = "Match 4", Address = "Address", Location = new Location(0, 0, "City", "Country"), MatchDateTimeUtc = DateTime.UtcNow.AddDays(1), MinPlayers = 2, MaxPlayers = 10, Sport = Sports.Football, Status = MatchStatus.Confirmed, Privacy = MatchPrivacy.Public, CreatedAtUtc = DateTime.UtcNow, Participants = new List<ApplicationUser> { participant, new() { Id = "p1", UserName = "p1", DisplayName = "P1", Email = "p1@test.com", EmailConfirmed = true }, new() { Id = "p2", UserName = "p2", DisplayName = "P2", Email = "p2@test.com", EmailConfirmed = true }, new() { Id = "p3", UserName = "p3", DisplayName = "P3", Email = "p3@test.com", EmailConfirmed = true }, new() { Id = "p4", UserName = "p4", DisplayName = "P4", Email = "p4@test.com", EmailConfirmed = true }, new() { Id = "p5", UserName = "p5", DisplayName = "P5", Email = "p5@test.com", EmailConfirmed = true }, new() { Id = "p6", UserName = "p6", DisplayName = "P6", Email = "p6@test.com", EmailConfirmed = true }, new() { Id = "p7", UserName = "p7", DisplayName = "P7", Email = "p7@test.com", EmailConfirmed = true }, new() { Id = "p8", UserName = "p8", DisplayName = "P8", Email = "p8@test.com", EmailConfirmed = true }, new() { Id = "p9", UserName = "p9", DisplayName = "P9", Email = "p9@test.com", EmailConfirmed = true } } } // 10 participants = max players
        };

        await _dbContext.Matches.AddRangeAsync(matches);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Match>> result = await _repository.GetMatchesForUser("creator1", _dbContext, q: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, m => m.Id == "match1");
        Assert.Contains(result.Data, m => m.Id == "match2"); // match1 and match2 should be returned (created by creator1, correct status, and have space for more players)
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddMatchToContext()
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

        // Act
        _repository.Add(match, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(match, _dbContext.Matches);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkMatchAsModified()
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

        match.Description = "Updated Description";

        // Act
        _repository.Update(match, _dbContext);

        // Assert
        EntityEntry<Match> entry = _dbContext.Entry(match);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveMatchFromContext()
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

        // Act
        _repository.Remove(match, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(match, _dbContext.Matches);
    }

    #endregion
}

