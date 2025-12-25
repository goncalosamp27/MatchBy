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

    #region GetAvailableTeamsAsync Tests

    [Fact]
    public async Task GetAvailableTeamsAsync_WithUser_ShouldReturnTeamsUserDoesNotOwnOrParticipateIn()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        var otherUser = new ApplicationUser { Id = "other1", UserName = "other1", DisplayName = "Other", Email = "other@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member, otherUser);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description 1", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "other1", Name = "Team 2", Description = "Description 2", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member } },
            new() { Id = "team3", OwnerId = "other1", Name = "Team 3", Description = "Description 3", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            UserId = "member1",
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetAvailableTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, t => t.Id == "team1");
        Assert.Contains(result.Data, t => t.Id == "team3"); // team1 and team3 should be available (not owned by member1 and member1 is not a member)
    }

    #endregion

    #region GetTeamsUserParticipateAsync Tests

    [Fact]
    public async Task GetTeamsUserParticipateAsync_WithMember_ShouldReturnTeamsUserParticipatesIn()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description 1", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member } },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description 2", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "member1", Name = "Team 3", Description = "Description 3", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member } }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsUserParticipateAsync("member1", page: 1, pageSize: 10, q: "", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(t => t.Members.Any(m => m.Id == "member1")));
    }

    #endregion

    #region GetTeams Advanced Filtering Tests

    [Fact]
    public async Task GetTeams_WithNameSortingAscending_ShouldReturnSortedTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Z Team", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "A Team", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "M Team", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
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
    }

    [Fact]
    public async Task GetTeams_WithDescriptionSortingDescending_ShouldReturnSortedTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "A Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Z Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "M Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Description,
            OrderBy = OrderBy.Descending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTeams_WithCreatedAtSorting_ShouldReturnSortedTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        DateTime baseTime = DateTime.UtcNow;
        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = baseTime.AddHours(2), Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = baseTime.AddHours(1), Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = baseTime, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.CreatedAt,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTeams_WithMembersCountSorting_ShouldReturnSortedTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member1 = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member 1", Email = "member1@test.com", EmailConfirmed = true };
        var member2 = new ApplicationUser { Id = "member2", UserName = "member2", DisplayName = "Member 2", Email = "member2@test.com", EmailConfirmed = true };
        var member3 = new ApplicationUser { Id = "member3", UserName = "member3", DisplayName = "Member 3", Email = "member3@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member1, member2, member3);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member1 } },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member1, member2, member3 } }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.MembersCount,
            OrderBy = OrderBy.Descending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal("team3", result.Data[0].Id); // 3 members
        Assert.Equal("team2", result.Data[1].Id); // 1 member
        Assert.Equal("team1", result.Data[2].Id); // 0 members
    }

    [Fact]
    public async Task GetTeams_WithPrivacyFilterPublic_ShouldReturnOnlyPublicTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description", Privacy = TeamPrivacy.Private, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.Public
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(t => t.Privacy == TeamPrivacy.Public));
    }

    [Fact]
    public async Task GetTeams_WithPrivacyFilterPrivate_ShouldReturnPrivateTeamsForMembers()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description", Privacy = TeamPrivacy.Private, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member } },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description", Privacy = TeamPrivacy.Private, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            UserId = "member1",
            Page = 1,
            PageSize = 10,
            Query = "",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.Private
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("team1", result.Data[0].Id); // Only the team where member1 is a member
    }

    [Fact]
    public async Task GetTeams_WithPrivacyFilterAll_ShouldReturnAllAccessibleTeams()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        var member = new ApplicationUser { Id = "member1", UserName = "member1", DisplayName = "Member", Email = "member@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(owner, member);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Team 1", Description = "Description", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Team 2", Description = "Description", Privacy = TeamPrivacy.Private, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser> { member } },
            new() { Id = "team3", OwnerId = "owner1", Name = "Team 3", Description = "Description", Privacy = TeamPrivacy.Private, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            UserId = "member1",
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
        Assert.Equal(2, result.Data.Count); // Public team + private team where user is member
        Assert.Contains(result.Data, t => t.Id == "team1");
        Assert.Contains(result.Data, t => t.Id == "team2");
    }

    [Fact]
    public async Task GetTeams_WithSearchQuery_ShouldFilterByNameAndDescription()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>
        {
            new() { Id = "team1", OwnerId = "owner1", Name = "Football Team", Description = "Playing football", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team2", OwnerId = "owner1", Name = "Basketball Team", Description = "Playing basketball", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() },
            new() { Id = "team3", OwnerId = "owner1", Name = "Soccer Club", Description = "Soccer activities", Privacy = TeamPrivacy.Public, MaxMembers = 10, CreatedAtUtc = DateTime.UtcNow, Members = new List<ApplicationUser>() }
        };

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 1,
            PageSize = 10,
            Query = "football",
            SortBy = SortBy.Name,
            OrderBy = OrderBy.Ascending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTeams_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(owner);
        await _dbContext.SaveChangesAsync();

        var teams = new List<Team>();
        for (int i = 1; i <= 7; i++)
        {
            teams.Add(new Team
            {
                Id = $"team{i}",
                OwnerId = "owner1",
                Name = $"Team {i}",
                Description = $"Description {i}",
                Privacy = TeamPrivacy.Public,
                MaxMembers = 10,
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                Members = new List<ApplicationUser>()
            });
        }

        await _dbContext.Teams.AddRangeAsync(teams);
        await _dbContext.SaveChangesAsync();

        var queryParams = new TeamQueryParametersDto
        {
            Page = 2,
            PageSize = 3,
            Query = "",
            SortBy = SortBy.CreatedAt,
            OrderBy = OrderBy.Descending,
            Privacy = Privacy.All
        };

        // Act
        PaginationResponse<List<Team>> result = await _repository.GetTeamsAsync(queryParams, new List<string>(), _dbContext);

        // Assert
        Assert.NotNull(result);
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

