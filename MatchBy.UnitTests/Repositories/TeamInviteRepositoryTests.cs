using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Repositories.TeamInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class TeamInviteRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TeamInviteRepository _repository;

    public TeamInviteRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new TeamInviteRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnTeamInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var invite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        TeamInvite? result = await _repository.GetByIdAsync("invite1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invite1", result.Id);
        Assert.Equal("sender1", result.SenderId);
        Assert.Equal("receiver1", result.ReceiverId);
        Assert.Equal("team1", result.TeamId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        TeamInvite? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ExistsPendingInviteByTeamAndUser Tests

    [Fact]
    public async Task ExistsPendingInviteByTeamAndUser_WithExistingPendingInvite_ShouldReturnTrue()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var pendingInvite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(pendingInvite);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.ExistsPendingInviteByTeamAndUser("team1", "receiver1", _dbContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsPendingInviteByTeamAndUser_WithAcceptedInvite_ShouldReturnFalse()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var acceptedInvite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Accepted,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(acceptedInvite);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.ExistsPendingInviteByTeamAndUser("team1", "receiver1", _dbContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsPendingInviteByTeamAndUser_WithExpiredInvite_ShouldReturnFalse()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var expiredInvite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1), // Expired
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(expiredInvite);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.ExistsPendingInviteByTeamAndUser("team1", "receiver1", _dbContext);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsPendingInviteByTeamAndUser_WithNonExistentInvite_ShouldReturnFalse()
    {
        // Act
        bool result = await _repository.ExistsPendingInviteByTeamAndUser("team1", "receiver1", _dbContext);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetInvites Tests

    [Fact]
    public async Task GetInvites_WithExistingInvites_ShouldReturnPagedInvites()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var invites = new List<TeamInvite>
        {
            new() { Id = "invite1", Content = "Join us!", SenderId = "sender1", ReceiverId = "receiver1", TeamId = "team1", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite2", Content = "Join us!", SenderId = "sender1", ReceiverId = "receiver1", TeamId = "team1", Status = InviteStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.TeamInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<TeamInvite>> result = await _repository.GetInvites("team1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetInvites_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var owner = new ApplicationUser { Id = "owner1", UserName = "owner1", DisplayName = "Owner", Email = "owner@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, owner);
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

        var invites = new List<TeamInvite>();
        for (int i = 1; i <= 5; i++)
        {
            invites.Add(new TeamInvite
            {
                Id = $"invite{i}",
                Content = "Join us!",
                SenderId = "sender1",
                ReceiverId = "receiver1",
                TeamId = "team1",
                Status = InviteStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _dbContext.TeamInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<TeamInvite>> result = await _repository.GetInvites("team1", _dbContext, page: 2, pageSize: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetInvites_WithNoInvites_ShouldReturnEmptyList()
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
        PaginationResponse<List<TeamInvite>> result = await _repository.GetInvites("team1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddTeamInviteToContext()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(invite, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(invite, _dbContext.TeamInvites);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkTeamInviteAsModified()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        invite.Status = InviteStatus.Accepted;

        // Act
        _repository.Update(invite, _dbContext);

        // Assert
        EntityEntry<TeamInvite> entry = _dbContext.Entry(invite);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveTeamInviteFromContext()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            Content = "Join our team!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            TeamId = "team1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(invite, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(invite, _dbContext.TeamInvites);
    }

    #endregion
}