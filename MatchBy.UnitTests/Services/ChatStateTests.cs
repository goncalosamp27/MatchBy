using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;

namespace MatchBy.UnitTests.Services;

public class ChatStateTests
{
    [Fact]
    public void InitUser_WhenCalled_ShouldSetUserId()
    {
        // Arrange
        var state = new ChatState();
        const string userId = "user-123";

        // Act
        state.InitUser(userId);

        // Assert
        Assert.Equal(userId, state.UserId);
    }

    [Fact]
    public void Select_WhenCalled_ShouldSetSelectedConversationAndMessages()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        var messages = new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [CreateChatMessageDto("msg-1", "conv-1")],
            NextCursor = "cursor-123"
        };

        // Act
        state.Select(conversation, messages);

        // Assert
        Assert.NotNull(state.Selected);
        Assert.Equal("conv-1", state.Selected.Id);
        Assert.Single(state.MessagesOfSelectedConversation);
        Assert.Equal("cursor-123", state.NextChatMessagesCursor);
    }

    [Fact]
    public void Select_WhenCalled_ShouldRaiseChangedEvent()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        bool eventRaised = false;
        state.Changed += (_, _) => eventRaised = true;
        ConversationDto conversation = CreateConversationDto("conv-1");
        var messages = new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        };

        // Act
        state.Select(conversation, messages);

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void UpsertMessage_WhenMessageExists_ShouldUpdateMessage()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [CreateChatMessageDto("msg-1", "conv-1", "Original content")],
            NextCursor = null
        });

        ChatMessageDto updatedMessage = CreateChatMessageDto("msg-1", "conv-1", "Updated content");

        // Act
        state.UpsertMessage(updatedMessage);

        // Assert
        Assert.Single(state.MessagesOfSelectedConversation);
        Assert.Equal("Updated content", state.MessagesOfSelectedConversation[0].Content);
    }

    [Fact]
    public void UpsertMessage_WhenMessageDoesNotExist_ShouldAddMessage()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        });

        ChatMessageDto newMessage = CreateChatMessageDto("msg-1", "conv-1");

        // Act
        state.UpsertMessage(newMessage);

        // Assert
        Assert.Single(state.MessagesOfSelectedConversation);
        Assert.Equal("msg-1", state.MessagesOfSelectedConversation[0].Id);
    }

    [Fact]
    public void UpsertMessage_WhenConversationNotFound_ShouldNotAddMessage()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ChatMessageDto message = CreateChatMessageDto("msg-1", "non-existent-conv");

        // Act
        state.UpsertMessage(message);

        // Assert
        Assert.Empty(state.MessagesOfSelectedConversation);
    }

    [Fact]
    public void UpsertMessage_WhenNotSelectedConversation_ShouldUpdateConversationButNotMessages()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        });

        ConversationDto otherConversation = CreateConversationDto("conv-2");
        state.Conversations.Add(otherConversation);
        ChatMessageDto message = CreateChatMessageDto("msg-1", "conv-2");

        // Act
        state.UpsertMessage(message);

        // Assert
        Assert.Empty(state.MessagesOfSelectedConversation);
        Assert.Equal("msg-1 content", state.Conversations[1].LastMessageContent);
    }

    [Fact]
    public void UpsertMessage_ShouldUpdateConversationLastMessage()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        ChatMessageDto message = CreateChatMessageDto("msg-1", "conv-1", "New message");

        // Act
        state.UpsertMessage(message);

        // Assert
        Assert.NotNull(state.Conversations[0].LastMessageAtUtc);
        Assert.Equal("New message", state.Conversations[0].LastMessageContent);
    }

    [Fact]
    public void RemoveMessage_WhenSelectedConversation_ShouldRemoveMessage()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [CreateChatMessageDto("msg-1", "conv-1"), CreateChatMessageDto("msg-2", "conv-1")],
            NextCursor = null
        });

        ConversationDto updatedConversation = conversation with { LastMessageContent = "Updated" };

        // Act
        state.RemoveMessage(updatedConversation, "msg-1");

        // Assert
        Assert.Single(state.MessagesOfSelectedConversation);
        Assert.Equal("msg-2", state.MessagesOfSelectedConversation[0].Id);
        Assert.Equal(updatedConversation, state.Selected);
    }

    [Fact]
    public void AddMessages_ShouldInsertMessagesAtBeginning()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        state.MessagesOfSelectedConversation = [CreateChatMessageDto("msg-1", "conv-1")];
        var newMessages = new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [CreateChatMessageDto("msg-2", "conv-1"), CreateChatMessageDto("msg-3", "conv-1")],
            NextCursor = "cursor-456"
        };

        // Act
        state.AddMessages(newMessages);

        // Assert
        Assert.Equal(3, state.MessagesOfSelectedConversation.Count);
        Assert.Equal("msg-2", state.MessagesOfSelectedConversation[0].Id);
        Assert.Equal("msg-1", state.MessagesOfSelectedConversation[2].Id);
        Assert.Equal("cursor-456", state.NextChatMessagesCursor);
    }

    [Fact]
    public void AddConversations_ShouldAddConversations()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        var conversations = new CursorPaginationResponse<List<ConversationDto>>
        {
            Data = [CreateConversationDto("conv-1"), CreateConversationDto("conv-2")],
            NextCursor = "cursor-789"
        };

        // Act
        state.AddConversations(conversations);

        // Assert
        Assert.Equal(2, state.Conversations.Count);
        Assert.Equal("cursor-789", state.NextConversationCursor);
    }

    [Fact]
    public void ClearConversations_ShouldRemoveAllConversations()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        state.Conversations.AddRange([CreateConversationDto("conv-1"), CreateConversationDto("conv-2")]);

        // Act
        state.ClearConversations();

        // Assert
        Assert.Empty(state.Conversations);
    }

    [Fact]
    public void RemoveConversation_WhenSelected_ShouldClearSelection()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        });

        // Act
        state.RemoveConversation("conv-1");

        // Assert
        Assert.Empty(state.Conversations);
        Assert.Null(state.Selected);
    }

    [Fact]
    public void RemoveConversation_WhenNotSelected_ShouldOnlyRemoveConversation()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conv1 = CreateConversationDto("conv-1");
        ConversationDto conv2 = CreateConversationDto("conv-2");
        state.Conversations.AddRange([conv1, conv2]);
        state.Select(conv1, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        });

        // Act
        state.RemoveConversation("conv-2");

        // Assert
        Assert.Single(state.Conversations);
        Assert.NotNull(state.Selected);
        Assert.Equal("conv-1", state.Selected.Id);
    }

    [Fact]
    public void UpdateConversation_WhenExists_ShouldUpdateConversation()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        ConversationDto updated = conversation with { Title = "Updated Title" };

        // Act
        state.UpdateConversation(updated);

        // Assert
        Assert.Equal("Updated Title", state.Conversations[0].Title);
    }

    [Fact]
    public void UpdateConversation_WhenNotExists_ShouldAddConversation()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");

        // Act
        state.UpdateConversation(conversation);

        // Assert
        Assert.Single(state.Conversations);
        Assert.Equal("conv-1", state.Conversations[0].Id);
    }

    [Fact]
    public void UpdateConversation_WhenSelected_ShouldUpdateSelected()
    {
        // Arrange
        var state = new ChatState();
        state.InitUser("user-123");
        ConversationDto conversation = CreateConversationDto("conv-1");
        state.Conversations.Add(conversation);
        state.Select(conversation, new CursorPaginationResponse<List<ChatMessageDto>>
        {
            Data = [],
            NextCursor = null
        });
        ConversationDto updated = conversation with { Title = "Updated Title" };

        // Act
        state.UpdateConversation(updated);

        // Assert
        Assert.NotNull(state.Selected);
        Assert.Equal("Updated Title", state.Selected.Title);
    }

    private static ConversationDto CreateConversationDto(string id, string? title = null)
    {
        return new ConversationDto
        {
            Id = id,
            Type = ConversationType.Private,
            Title = title ?? $"Conversation {id}",
            CreatorId = "creator-123",
            CreatedAtUtc = DateTime.UtcNow,
            MessagesCount = 0,
            Participants = []
        };
    }

    private static ChatMessageDto CreateChatMessageDto(string id, string conversationId, string? content = null)
    {
        return new ChatMessageDto
        {
            Id = id,
            Content = content ?? $"{id} content",
            SenderId = "sender-123",
            Sender = new UserDto
            {
                Id = "sender-123",
                DisplayName = "Sender",
                AvatarUrl = null
            },
            ConversationId = conversationId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}

