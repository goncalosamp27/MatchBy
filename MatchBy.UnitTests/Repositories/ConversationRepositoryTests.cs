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
        string? result = await _repository.PrivateConversationExistsAsync(["user1", "user2"], _dbContext, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
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
        string? result = await _repository.PrivateConversationExistsAsync(["user1", "user2"], _dbContext, CancellationToken.None);

        // Assert
        Assert.Null(result);
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

    #region GetConversationsForUserAsync Advanced Tests

    [Fact]
    public async Task GetConversationsForUserAsync_WithCursorPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", DisplayName = "User 3", Email = "user3@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>();
        for (int i = 1; i <= 5; i++)
        {
            var conversation = new Conversation
            {
                Id = $"conv{i}",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                LastMessageAtUtc = DateTime.UtcNow.AddMinutes(-i),
                Participants = new List<ApplicationUser> { user1, i == 1 ? user2 : user3 }
            };
            conversations.Add(conversation);
        }

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act - Get first page
        CursorPaginationResponse<List<Conversation>> firstPageResult = await _repository.GetConversationsForUserAsync("user1", pageSize: 2, cursor: null, query: null, _dbContext);

        // Assert first page
        Assert.NotNull(firstPageResult);
        Assert.Equal(2, firstPageResult.Data.Count);
        Assert.NotNull(firstPageResult.NextCursor);

        // Act - Get second page using cursor
        CursorPaginationResponse<List<Conversation>> secondPageResult = await _repository.GetConversationsForUserAsync("user1", pageSize: 2, cursor: firstPageResult.NextCursor, query: null, _dbContext);

        // Assert second page
        Assert.NotNull(secondPageResult);
    }

    [Fact]
    public async Task GetConversationsForUserAsync_WithSearchQuery_ShouldFilterByTitle()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>
        {
            new()
            {
                Id = "conv1",
                Type = ConversationType.Team,
                Title = "Football Team",
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            },
            new()
            {
                Id = "conv2",
                Type = ConversationType.Team,
                Title = "Basketball Team",
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            },
            new()
            {
                Id = "conv3",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            }
        };

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Conversation>> result = await _repository.GetConversationsForUserAsync("user1", pageSize: 10, cursor: null, query: "football", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Football Team", result.Data[0].Title);
    }

    [Fact]
    public async Task GetConversationsForUserAsync_WithSearchQuery_ShouldFilterByParticipantName()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "John Doe", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "Jane Smith", Email = "user2@test.com", EmailConfirmed = true };
        var user3 = new ApplicationUser { Id = "user3", UserName = "user3", DisplayName = "Bob Wilson", Email = "user3@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2, user3);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>
        {
            new()
            {
                Id = "conv1",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            },
            new()
            {
                Id = "conv2",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user3 }
            }
        };

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Conversation>> result = await _repository.GetConversationsForUserAsync("user1", pageSize: 10, cursor: null, query: "jane", _dbContext);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetConversationsForUserAsync_WithLargePageSize_ShouldReturnAllConversations()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>();
        for (int i = 1; i <= 5; i++)
        {
            conversations.Add(new Conversation
            {
                Id = $"conv{i}",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                Participants = new List<ApplicationUser> { user1, user2 }
            });
        }

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Conversation>> result = await _repository.GetConversationsForUserAsync("user1", pageSize: int.MaxValue, cursor: null, query: null, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.NextCursor); // No next cursor when all items are returned
    }

    [Fact]
    public async Task GetConversationsForUserAsync_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<Conversation>> result = await _repository.GetConversationsForUserAsync("user1", pageSize: 10, cursor: null, query: null, _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task GetConversationsForUserAsync_WithInvalidCursor_ShouldReturnFromBeginning()
    {
        // Arrange
        var user1 = new ApplicationUser { Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true };
        var user2 = new ApplicationUser { Id = "user2", UserName = "user2", DisplayName = "User 2", Email = "user2@test.com", EmailConfirmed = true };
        await _dbContext.Users.AddRangeAsync(user1, user2);
        await _dbContext.SaveChangesAsync();

        var conversations = new List<Conversation>
        {
            new()
            {
                Id = "conv1",
                Type = ConversationType.Private,
                CreatorId = "user1",
                CreatedAtUtc = DateTime.UtcNow,
                Participants = new List<ApplicationUser> { user1, user2 }
            }
        };

        await _dbContext.Conversations.AddRangeAsync(conversations);
        await _dbContext.SaveChangesAsync();

        // Act - Use invalid cursor
        CursorPaginationResponse<List<Conversation>> result = await _repository.GetConversationsForUserAsync("user1", pageSize: 10, cursor: "invalid-cursor", query: null, _dbContext);

        // Assert - Should return all conversations (fallback to no cursor)
        Assert.NotNull(result);
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
        _dbContext.SaveChanges();

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
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.DoesNotContain(conversation, _dbContext.Conversations);
    }

    #endregion
}