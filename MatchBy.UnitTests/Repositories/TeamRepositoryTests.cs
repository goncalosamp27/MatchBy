using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Repositories.Team;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class TeamRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamRepository _repository;

    public TeamRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new TeamRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithPublicTeam_ShouldReturnTeam()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Team? result = await _repository.GetByIdAsync("team1", "user1", false, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("team1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithPrivateTeamAndMember_ShouldReturnTeam()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Private,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { member }
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Team? result = await _repository.GetByIdAsync("team1", "member1", false, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("team1", result.Id);
    }

    #endregion

    #region GetTeamUserOwnsByIdAsync Tests

    [Fact]
    public async Task GetTeamUserOwnsByIdAsync_WithOwner_ShouldReturnTeam()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Team? result = await _repository.GetTeamUserOwnsByIdAsync("team1", "owner1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("team1", result.Id);
        Assert.Equal("owner1", result.OwnerId);
    }

    [Fact]
    public async Task GetTeamUserOwnsByIdAsync_WithNonOwner_ShouldReturnNull()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var user = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User", Email = "user@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, user);
        await _dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Team? result = await _repository.GetTeamUserOwnsByIdAsync("team1", "user1", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetTeamUserParticipatesByIdAsync Tests

    [Fact]
    public async Task GetTeamUserParticipatesByIdAsync_WithMember_ShouldReturnTeam()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.SaveChangesAsync();

        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser> { member }
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        Team? result = await _repository.GetTeamUserParticipatesByIdAsync("team1", "member1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("team1", result.Id);
        Assert.Contains(member, result.Members);
    }

    #endregion

    #region GetTeamsAsync Tests

    [Fact]
    public async Task GetTeamsAsync_WithBasicQuery_ShouldReturnTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description 1", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description 2", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            UserId = "owner1",
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
    }

    #endregion

    #region GetTeamsUserOwnAsync Tests

    [Fact]
    public async Task GetTeamsUserOwnAsync_WithOwner_ShouldReturnTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description 1", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description 2", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsUserOwnAsync("owner1", page: 1, pageSize: 10, q: "", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(t => t.OwnerId == "owner1"));
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddTeamToContext()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        // Act
        _repository.Add(team, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(team, _dbContext.Teams);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkTeamAsModified()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        team.Name = "Updated Name";

        // Act
        _repository.Update(team, _dbContext);

        // Assert
        EntityEntry<Team> entry = _dbContext.Entry(team);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveTeamFromContext()
    {
        // Arrange
        var team = new Team
        {
            Id = "team1",
            OwnerId = "owner1",
            Name = "Test Team",
            Description = "Test Description",
            Privacy = TeamPrivacy.Public,
            MaxMembers = 10,
            CreatedAtUtc = DateTime.UtcNow,
            Members = new List<ApplicationUser>()
        };

        await _dbContext.Teams.AddAsync(team);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(team, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(team, _dbContext.Teams);
    }

    #endregion
}

