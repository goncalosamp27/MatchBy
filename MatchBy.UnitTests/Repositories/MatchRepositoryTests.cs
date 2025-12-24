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

        // Assert
        Assert.DoesNotContain(match, _dbContext.Matches);
    }

    #endregion
}

