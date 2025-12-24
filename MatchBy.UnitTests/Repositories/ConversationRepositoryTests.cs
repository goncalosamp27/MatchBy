using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class ConversationRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ConversationRepository _repository;

    public ConversationRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new ConversationRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidIdAndParticipant_ShouldReturnConversation()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Conversation? result = await _repository.GetByIdAsync("conv1", "user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("conv1", result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonParticipant_ShouldReturnNull()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        var user3 = new ApplicationUser
        {
            Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Conversation? result = await _repository.GetByIdAsync("conv1", "user3", _dbContext);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithPrivateConversation_ShouldSetTitleToOtherParticipantName()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        Conversation? result = await _repository.GetByIdAsync("conv1", "user1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("User 2", result.Title);
    }

    #endregion

    #region IsParticipantAsync Tests

    [Fact]
    public async Task IsParticipantAsync_WithParticipant_ShouldReturnTrue()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.IsParticipantAsync("conv1", "user1", _dbContext);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsParticipantAsync_WithNonParticipant_ShouldReturnFalse()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.IsParticipantAsync("conv1", "user2", _dbContext);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region PrivateConversationExistsAsync Tests

    [Fact]
    public async Task PrivateConversationExistsAsync_WithExistingConversation_ShouldReturnTrue()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result =
            await _repository.PrivateConversationExistsAsync(["user1", "user2"], _dbContext, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PrivateConversationExistsAsync_WithNonExistentConversation_ShouldReturnFalse()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result =
            await _repository.PrivateConversationExistsAsync(["user1", "user2"], _dbContext, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CanDeleteConversation Tests

    [Fact]
    public async Task CanDeleteConversation_WithPrivateConversationAndParticipant_ShouldReturnTrue()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.CanDeleteConversation("conv1", ConversationType.Private, "user1", _dbContext,
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanDeleteConversation_WithTeamConversationAndCreator_ShouldReturnTrue()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.CanDeleteConversation("conv1", ConversationType.Team, "user1", _dbContext,
            CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanDeleteConversation_WithTeamConversationAndNonCreator_ShouldReturnFalse()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Team,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1, user2 }
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        bool result = await _repository.CanDeleteConversation("conv1", ConversationType.Team, "user2", _dbContext,
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetConversationsForUserAsync Tests

    [Fact]
    public async Task GetConversationsForUserAsync_WithValidUserId_ShouldReturnConversations()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var user2 = new ApplicationUser
        {
            Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true
        };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>
        {
            new()
            {
                Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            },
            new()
            {
                Id = "conv2", Type = ConversationType.Private, CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1), Participants = new List<ApplicationUser> { user1 }
            }
        };

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Conversation>> result =
            await _repository.GetConversationsForUserAsync("user1", pageSize: 10, cursor: null, query: null,
                _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddConversationToContext()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };

        // Act
        _repository.Add(conversation, _dbContext);

        // Assert
        Assert.Contains(conversation, _dbContext.Conversations);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkConversationAsModified()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        conversation.Title = "Updated Title";

        // Act
        _repository.Update(conversation, _dbContext);

        // Assert
        EntityEntry<Conversation> entry = _dbContext.Entry(conversation);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveConversationFromContext()
    {
        // Arrange
        var conversation = new Conversation
        {
            Id = "conv1",
            Type = ConversationType.Private,
            CreatorId = "user1",
            CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser>()
        };

        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(conversation, _dbContext);

        // Assert
        Assert.DoesNotContain(conversation, _dbContext.Conversations);
    }

    #endregion
}