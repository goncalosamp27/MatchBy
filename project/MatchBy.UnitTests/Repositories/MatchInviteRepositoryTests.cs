using MatchBy.Data;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.MatchInvite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class MatchInviteRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly MatchInviteRepository _repository;

    public MatchInviteRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new MatchInviteRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetReceivedInvites Tests

    [Fact]
    public async Task GetReceivedInvites_WithExistingInvites_ShouldReturnPagedInvites()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, creator);
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

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver1", MatchId = "match1", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite2", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver1", MatchId = "match1", Status = InviteStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.MatchInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetReceivedInvites("receiver1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetReceivedInvites_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(receiver, creator);
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

        var invites = new List<MatchInvite>();
        for (int i = 1; i <= 5; i++)
        {
            var sender = new ApplicationUser { Id = $"sender{i}", UserName = $"sender{i}", DisplayName = $"Sender {i}", Email = $"sender{i}@test.com", EmailConfirmed = true };
            await _dbContext.Users.AddAsync(sender);

            invites.Add(new MatchInvite
            {
                Id = $"invite{i}",
                Content = "Join our match!",
                SenderId = $"sender{i}",
                ReceiverId = "receiver1",
                MatchId = "match1",
                Status = InviteStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _dbContext.MatchInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetReceivedInvites("receiver1", _dbContext, page: 2, pageSize: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetReceivedInvites_WithNoInvites_ShouldReturnEmptyList()
    {
        // Arrange
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(receiver);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetReceivedInvites("receiver1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalCount);
    }

    #endregion

    #region GetSentInvites Tests

    [Fact]
    public async Task GetSentInvites_WithExistingInvites_ShouldReturnPagedInvites()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, creator);
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

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver1", MatchId = "match1", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite2", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver2", MatchId = "match1", Status = InviteStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.MatchInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetSentInvites("sender1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    #endregion

    #region GetInvitesForMatch Tests

    [Fact]
    public async Task GetInvitesForMatch_WithExistingInvites_ShouldReturnPagedInvites()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, creator);
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

        var invites = new List<MatchInvite>
        {
            new() { Id = "invite1", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver1", MatchId = "match1", Status = InviteStatus.Pending, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow },
            new() { Id = "invite2", Content = "Join our match!", SenderId = "sender1", ReceiverId = "receiver2", MatchId = "match1", Status = InviteStatus.Accepted, ExpiresAtUtc = DateTime.UtcNow.AddDays(7), CreatedAtUtc = DateTime.UtcNow }
        };
        await _dbContext.MatchInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetInvitesForMatch("match1", _dbContext, page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnMatchInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, creator);
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

        var invite = new MatchInvite
        {
            Id = "invite1",
            Content = "Join our match!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        MatchInvite? result = await _repository.GetByIdAsync("invite1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invite1", result.Id);
        Assert.Equal("sender1", result.SenderId);
        Assert.Equal("receiver1", result.ReceiverId);
        Assert.Equal("match1", result.MatchId);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        MatchInvite? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithMatchAndReceiver_ExistingInvite_ShouldReturnMatchInvite()
    {
        // Arrange
        var sender = new ApplicationUser { Id = "sender1", UserName = "sender1", DisplayName = "Sender", Email = "sender@test.com", EmailConfirmed = true };
        var receiver = new ApplicationUser { Id = "receiver1", UserName = "receiver1", DisplayName = "Receiver", Email = "receiver@test.com", EmailConfirmed = true };
        var creator = new ApplicationUser { Id = "creator1", UserName = "creator1", DisplayName = "Creator", Email = "creator@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(sender, receiver, creator);
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

        var invite = new MatchInvite
        {
            Id = "invite1",
            Content = "Join our match!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        MatchInvite? result = await _repository.GetByIdAsync("match1", "receiver1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invite1", result.Id);
        Assert.Equal("match1", result.MatchId);
        Assert.Equal("receiver1", result.ReceiverId);
    }

    [Fact]
    public async Task GetByIdAsync_WithMatchAndReceiver_NonExistentInvite_ShouldReturnNull()
    {
        // Act
        MatchInvite? result = await _repository.GetByIdAsync("match1", "receiver1", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddMatchInviteToContext()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            Content = "Join our match!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(invite, _dbContext);
        _dbContext.SaveChanges();

        // Assert
        Assert.Contains(invite, _dbContext.MatchInvites);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkMatchInviteAsModified()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            Content = "Join our match!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        invite.Status = InviteStatus.Accepted;

        // Act
        _repository.Update(invite, _dbContext);

        // Assert
        EntityEntry<MatchInvite> entry = _dbContext.Entry(invite);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveMatchInviteFromContext()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            Content = "Join our match!",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            MatchId = "match1",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
            CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(invite, _dbContext);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(invite, _dbContext.MatchInvites);
    }

    #endregion
}