using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Repositories.ChatMessage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MatchBy.UnitTests.Repositories;

public class ChatMessageRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ChatMessageRepository _repository;

    public ChatMessageRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new ChatMessageRepository();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetChatMessagesAsync Tests

    [Fact]
    public async Task GetChatMessagesAsync_WithValidConversationId_ShouldReturnMessages()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var conversation = new Conversation
        {
            Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello",
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = "msg2", ConversationId = "conv1", SenderId = "user1", Content = "World",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        await _dbContext.ChatMessages.AddRangeAsync(messages);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<ChatMessage>> result =
            await _repository.GetChatMessagesAsync("conv1", "user1", _dbContext, pageSize: 10, cursor: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithCursor_ShouldReturnMessagesBeforeCursor()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var conversation = new Conversation
        {
            Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Message 1",
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = "msg2", ConversationId = "conv1", SenderId = "user1", Content = "Message 2",
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = "msg3", ConversationId = "conv1", SenderId = "user1", Content = "Message 3",
                CreatedAtUtc = DateTime.UtcNow
            }
        };

        await _dbContext.ChatMessages.AddRangeAsync(messages);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<ChatMessage>> result =
            await _repository.GetChatMessagesAsync("conv1", "user1", _dbContext, pageSize: 2, cursor: "msg3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.True(result.Data.All(m => string.Compare(m.Id, "msg3", StringComparison.Ordinal) < 0));
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithPageSize_ShouldLimitResults()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var conversation = new Conversation
        {
            Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var messages = new List<ChatMessage>();
        for (int i = 1; i <= 5; i++)
        {
            messages.Add(new ChatMessage
            {
                Id = $"msg{i}", ConversationId = "conv1", SenderId = "user1", Content = $"Message {i}",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.ChatMessages.AddRangeAsync(messages);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<ChatMessage>> result =
            await _repository.GetChatMessagesAsync("conv1", "user1", _dbContext, pageSize: 3, cursor: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.NotNull(result.NextCursor);
    }

    [Fact]
    public async Task GetChatMessagesAsync_WithNoMessages_ShouldReturnEmptyList()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var conversation = new Conversation
        {
            Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        // Act
        CursorPaginationResponse<List<ChatMessage>> result =
            await _repository.GetChatMessagesAsync("conv1", "user1", _dbContext, pageSize: 10, cursor: null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Null(result.NextCursor);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnMessage()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user1", UserName = "user1", DisplayName = "User 1", Email = "user1@test.com", EmailConfirmed = true
        };
        var conversation = new Conversation
        {
            Id = "conv1", Type = ConversationType.Private, CreatorId = "user1", CreatedAtUtc = DateTime.UtcNow,
            Participants = new List<ApplicationUser> { user1 }
        };
        await _dbContext.Users.AddAsync(user1);
        await _dbContext.Conversations.AddAsync(conversation);
        await _dbContext.SaveChangesAsync();

        var message = new ChatMessage
        {
            Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        ChatMessage? result = await _repository.GetByIdAsync("msg1", _dbContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg1", result.Id);
        Assert.Equal("Hello", result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        ChatMessage? result = await _repository.GetByIdAsync("nonexistent", _dbContext);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_ShouldAddMessageToContext()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        _repository.Add(message, _dbContext);

        // Assert
        Assert.Contains(message, _dbContext.ChatMessages);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldMarkMessageAsModified()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        message.Content = "Updated";

        // Act
        _repository.Update(message, _dbContext);

        // Assert
        EntityEntry<ChatMessage> entry = _dbContext.Entry(message);
        Assert.Equal(EntityState.Modified, entry.State);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public async Task Remove_ShouldRemoveMessageFromContext()
    {
        // Arrange
        var message = new ChatMessage
        {
            Id = "msg1", ConversationId = "conv1", SenderId = "user1", Content = "Hello", CreatedAtUtc = DateTime.UtcNow
        };
        await _dbContext.ChatMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        _repository.Remove(message, _dbContext);

        // Assert
        Assert.DoesNotContain(message, _dbContext.ChatMessages);
    }

    #endregion
}