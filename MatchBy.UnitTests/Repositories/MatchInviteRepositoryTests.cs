using MatchBy.Data;
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
    public async Task GetReceivedInvites_WithValidUserId_ShouldReturnInvites()
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
            Sport = MatchBy.Enums.Sports.Football,
            Status = MatchBy.Enums.MatchStatus.Pendent,
            Privacy = MatchBy.Enums.MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetReceivedInvites("receiver1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("receiver1", result.Data[0].ReceiverId);
    }

    [Fact]
    public async Task GetReceivedInvites_WithPagination_ShouldReturnCorrectPage()
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
            Sport = MatchBy.Enums.Sports.Football,
            Status = MatchBy.Enums.MatchStatus.Pendent,
            Privacy = MatchBy.Enums.MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var invites = new List<MatchInvite>();
        for (int i = 1; i <= 10; i++)
        {
            invites.Add(new MatchInvite
            {
                Id = $"invite{i}",
                MatchId = "match1",
                SenderId = "sender1",
                ReceiverId = "receiver1",
                Content = $"Invite {i}",
                Status = InviteStatus.Pending,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await _dbContext.MatchInvites.AddRangeAsync(invites);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetReceivedInvites("receiver1", _dbContext, page: 2, pageSize: 3);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    #endregion

    #region GetSentInvites Tests

    [Fact]
    public async Task GetSentInvites_WithValidUserId_ShouldReturnInvites()
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
            Sport = MatchBy.Enums.Sports.Football,
            Status = MatchBy.Enums.MatchStatus.Pendent,
            Privacy = MatchBy.Enums.MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetSentInvites("sender1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("sender1", result.Data[0].SenderId);
    }

    #endregion

    #region GetInvitesForMatch Tests

    [Fact]
    public async Task GetInvitesForMatch_WithValidMatchId_ShouldReturnInvites()
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
            Sport = MatchBy.Enums.Sports.Football,
            Status = MatchBy.Enums.MatchStatus.Pendent,
            Privacy = MatchBy.Enums.MatchPrivacy.Public,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.Matches.AddAsync(match);
        await _dbContext.SaveChangesAsync();

        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        PaginationResponse<List<MatchInvite>> result = await _repository.GetInvitesForMatch("match1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("match1", result.Data[0].MatchId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithInviteId_ShouldReturnInvite()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        MatchInvite? result = await _repository.GetByIdAsync("invite1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("invite1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithCompositeKey_ShouldReturnInvite()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
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

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddInviteToContext()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(invite, _dbContext);

        // Assert
        Assert.Contains(invite, _dbContext.MatchInvites);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkInviteAsModified()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
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
    public async Task Remove_ShouldRemoveInviteFromContext()
    {
        // Arrange
        var invite = new MatchInvite
        {
            Id = "invite1",
            MatchId = "match1",
            SenderId = "sender1",
            ReceiverId = "receiver1",
            Content = "Invite",
            Status = InviteStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dbContext.MatchInvites.AddAsync(invite);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(invite, _dbContext);

        // Assert
        Assert.DoesNotContain(invite, _dbContext.MatchInvites);
    }

    #endregion
}

