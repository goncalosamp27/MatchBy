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
    public async Task GetByIdAsync_WithValidId_ShouldReturnInvite()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        TeamInvite? result = await _repository.GetByIdAsync("invite1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invite1", result.Id);
        Assert.Equal("team1", result.TeamId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        TeamInvite? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ExistsPendingInviteByTeamAndUser Tests

    [Fact]
    public async Task ExistsPendingInviteByTeamAndUser_WithPendingInvite_ShouldReturnTrue()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.TeamInvites.AddAsync(invite);
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
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Accepted,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.TeamInvites.AddAsync(invite);
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
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        await _dbContext.TeamInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.ExistsPendingInviteByTeamAndUser("team1", "receiver1", _dbContext);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetInvites Tests

    [Fact]
    public async Task GetInvites_WithValidTeamId_ShouldReturnInvites()
    {
        // Arrange
        var invites = new List<TeamInvite>
        {
            new() { Id = "invite1", TeamId = "team1", SenderId = "sender1", ReceiverId = "receiver1", Content = "Invite 1", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(1), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite2", TeamId = "team1", SenderId = "sender1", ReceiverId = "receiver2", Content = "Invite 2", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(1), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite3", TeamId = "team2", SenderId = "sender1", ReceiverId = "receiver3", Content = "Invite 3", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(1), CreatedAtUtc = DateTime.UtcNow }
        };

        await _dbContext.TeamInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<TeamInvite>> result = await _repository.GetInvites("team1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(i => i.TeamId == "team1"));
    }

    [Fact]
    public async Task GetInvites_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var invites = new List<TeamInvite>();
        for (int i = 1; i <= 10; i++)
        {
            invites.Add(new TeamInvite
            {
                Id = $"invite{i}",
                TeamId = "team1",
                SenderId = "sender1",
                ReceiverId = $"receiver{i}",
                Content = $"Invite {i}",
                Status = InviteStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await _dbContext.TeamInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<TeamInvite>> result = await _repository.GetInvites("team1", _dbContext, page: 2, pageSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddInviteToContext()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
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
    public async Task Update_ShouldMarkInviteAsModified()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
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
    public async Task Remove_ShouldRemoveInviteFromContext()
    {
        // Arrange
        var invite = new TeamInvite
        {
            Id = "invite1",
            TeamId = "team1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
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

