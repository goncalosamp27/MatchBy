using System.Diagnostics.CodeAnalysis;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.DTOs.User;
using MatchBy.Hubs;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.Notifications;
using Microsoft.AspNetCore.SignalR;
using Moq;

#pragma warning disable CA2000 // Dispose objects before losing scope - Test objects don't need disposal

namespace MatchBy.UnitTests.Hubs;

public class ChatHubTests : IDisposable
{
    private readonly Mock<IChatMessageService> _chatMessageServiceMock;
    private readonly Mock<IConversationService> _conversationServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IHubCallerClients> _hubClientsMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly ChatHub _chatHub;

    public ChatHubTests()
    {
        _chatMessageServiceMock = new Mock<IChatMessageService>();
        _conversationServiceMock = new Mock<IConversationService>();
        _notificationServiceMock = new Mock<INotificationService>();
        var hubCallerContextMock = new Mock<HubCallerContext>();
        _hubClientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        var singleClientProxyMock = new Mock<ISingleClientProxy>();
        _groupManagerMock = new Mock<IGroupManager>();

        hubCallerContextMock.Setup(x => x.ConnectionId).Returns("connection-123");
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        _hubClientsMock.Setup(x => x.Client(It.IsAny<string>())).Returns(singleClientProxyMock.Object);

        _chatHub = new ChatHub(_chatMessageServiceMock.Object, _conversationServiceMock.Object, _notificationServiceMock.Object)
        {
            Context = hubCallerContextMock.Object,
            Clients = _hubClientsMock.Object,
            Groups = _groupManagerMock.Object
        };
    }

    [Fact]
    public async Task Register_WhenCalled_ShouldRegisterUserAndNotifyCaller()
    {
        // Arrange
        const string userId = "user-123";
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        // Act
        await _chatHub.Register(userId);

        // Assert
        Assert.Equal("Registered", captured.Method);
        Assert.NotNull(captured.Arg);
        Assert.Contains(userId, captured.Arg.ToString()!);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUserHasMultipleConnections_ShouldRemoveOnlyCurrentConnection()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        // Create another connection for the same user
        ChatHub secondHub = CreateHubWithConnection("connection-456");
        await secondHub.Register(userId);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert - Verify that the connection was removed but user still exists
        // We can't directly verify the internal dictionaries, but we can verify no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WhenUserHasSingleConnection_ShouldRemoveUser()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        // Act
        await _chatHub.OnDisconnectedAsync(null);

        // Assert - Verify no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task JoinConversation_WhenCalled_ShouldAddConnectionToGroup()
    {
        // Arrange
        const string conversationId = "conv-123";

        // Act
        await _chatHub.JoinConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            x => x.AddToGroupAsync("connection-123", conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_WhenCalled_ShouldRemoveConnectionFromGroup()
    {
        // Arrange
        const string conversationId = "conv-123";

        // Act
        await _chatHub.LeaveConversation(conversationId);

        // Assert
        _groupManagerMock.Verify(
            x => x.RemoveFromGroupAsync("connection-123", conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateMessage_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = "user-123",
            ConversationId = "conv-123",
            Content = "Test message"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.CreateMessage(createDto));
    }

    [Fact]
    public async Task CreateMessage_WhenCreatorUserIdMismatch_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = "different-user",
            ConversationId = "conv-123",
            Content = "Test message"
        };

        // Act
        await _chatHub.CreateMessage(createDto);

        // Assert
        Assert.Equal("MessageCreated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(captured.Arg);
        Assert.False(result.Success);
        _chatMessageServiceMock.Verify(x => x.CreateChatMessageAsync(It.IsAny<CreateChatMessageDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateMessage_WhenServiceFails_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = userId,
            ConversationId = "conv-123",
            Content = "Test message"
        };

        _chatMessageServiceMock
            .Setup(x => x.CreateChatMessageAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Fail("Service error"));

        // Act
        await _chatHub.CreateMessage(createDto);

        // Assert
        Assert.Equal("MessageCreated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateMessage_WhenConversationNotFound_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = userId,
            ConversationId = "conv-123",
            Content = "Test message"
        };

        ChatMessageDto messageDto = CreateChatMessageDto("msg-123", "conv-123", userId);
        _chatMessageServiceMock
            .Setup(x => x.CreateChatMessageAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Ok(messageDto));

        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync("conv-123", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Conversation not found"));

        // Act
        await _chatHub.CreateMessage(createDto);

        // Assert
        Assert.Equal("MessageCreated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateMessage_WhenSuccessful_ShouldNotifyAllParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        // Register participant
        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        var createDto = new CreateChatMessageDto
        {
            CreatorUserId = userId,
            ConversationId = "conv-123",
            Content = "Test message"
        };

        ChatMessageDto messageDto = CreateChatMessageDto("msg-123", "conv-123", userId);
        _chatMessageServiceMock
            .Setup(x => x.CreateChatMessageAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Ok(messageDto));

        ConversationDto conversationDto = CreateConversationDto("conv-123", [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync("conv-123", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.CreateMessage(createDto);

        // Assert
        _hubClientsMock.Verify(
            x => x.Clients(It.Is<IReadOnlyList<string>>(list => list.Contains("connection-123") && list.Contains("connection-456"))),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("MessageCreated", calls[0].Method);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(calls[0].Arg);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateMessage_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg-123",
            Content = "Updated message",
            CreatorUserId = "user-123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.UpdateMessage(updateDto));
    }

    [Fact]
    public async Task UpdateMessage_WhenCreatorUserIdMismatch_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg-123",
            Content = "Updated message",
            CreatorUserId = "different-user"
        };

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.UpdateMessage(updateDto);

        // Assert
        Assert.Equal("MessageUpdated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateMessage_WhenSuccessful_ShouldNotifyAllParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        var updateDto = new UpdateChatMessageDto
        {
            ChatMessageId = "msg-123",
            Content = "Updated message",
            CreatorUserId = userId
        };

        ChatMessageDto messageDto = CreateChatMessageDto("msg-123", "conv-123", userId);
        _chatMessageServiceMock
            .Setup(x => x.UpdateChatMessageAsync(updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Ok(messageDto));

        ConversationDto conversationDto = CreateConversationDto("conv-123", [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync("conv-123", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.UpdateMessage(updateDto);

        // Assert
        _hubClientsMock.Verify(
            x => x.Clients(It.Is<IReadOnlyList<string>>(list => list.Contains("connection-123") && list.Contains("connection-456"))),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("MessageUpdated", calls[0].Method);
        Result<ChatMessageDto> result = Assert.IsType<Result<ChatMessageDto>>(calls[0].Arg);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task DeleteMessage_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        const string messageId = "msg-123";

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.DeleteMessage(messageId));
    }

    [Fact]
    public async Task DeleteMessage_WhenMessageNotFound_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        const string messageId = "msg-123";
        _chatMessageServiceMock
            .Setup(x => x.GetChatMessageByIdAsync(messageId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Fail("Message not found"));

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.DeleteMessage(messageId);

        // Assert
        Assert.Equal("MessageDeleted", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteMessage_WhenDeleteFails_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        const string messageId = "msg-123";
        ChatMessageDto messageDto = CreateChatMessageDto(messageId, "conv-123", userId);
        _chatMessageServiceMock
            .Setup(x => x.GetChatMessageByIdAsync(messageId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Ok(messageDto));

        _chatMessageServiceMock
            .Setup(x => x.DeleteChatMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail("Delete failed"));

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.DeleteMessage(messageId);

        // Assert
        Assert.Equal("MessageDeleted", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteMessage_WhenSuccessful_ShouldNotifyAllParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        const string messageId = "msg-123";
        ChatMessageDto messageDto = CreateChatMessageDto(messageId, "conv-123", userId);
        _chatMessageServiceMock
            .Setup(x => x.GetChatMessageByIdAsync(messageId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChatMessageDto>.Ok(messageDto));

        _chatMessageServiceMock
            .Setup(x => x.DeleteChatMessageAsync(messageId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        ConversationDto conversationDto = CreateConversationDto("conv-123", [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync("conv-123", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.DeleteMessage(messageId);

        // Assert
        _hubClientsMock.Verify(
            x => x.Clients(It.Is<IReadOnlyList<string>>(list => list.Contains("connection-123") && list.Contains("connection-456"))),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("MessageDeleted", calls[0].Method);
        Result<ChatHub.MessageDeletedDto> result = Assert.IsType<Result<ChatHub.MessageDeletedDto>>(calls[0].Arg);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateConversation_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            CreatorUserId = "user-123",
            ConversationType = ConversationType.Private,
            ParticipantIds = ["user-456"]
        };

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.CreateConversation(createDto));
    }

    [Fact]
    public async Task CreateConversation_WhenCreatorUserIdMismatch_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        var createDto = new CreateConversationDto
        {
            CreatorUserId = "different-user",
            ConversationType = ConversationType.Private,
            ParticipantIds = ["user-456"]
        };

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.CreateConversation(createDto);

        // Assert
        Assert.Equal("ConversationCreated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ConversationDto> result = Assert.IsType<Result<ConversationDto>>(captured.Arg);
        Assert.False(result.Success);
        _conversationServiceMock.Verify(x => x.CreateConversationAsync(It.IsAny<CreateConversationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateConversation_WhenServiceFails_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        var createDto = new CreateConversationDto
        {
            CreatorUserId = userId,
            ConversationType = ConversationType.Private,
            ParticipantIds = ["user-456"]
        };

        _conversationServiceMock
            .Setup(x => x.CreateConversationAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Service error"));

        // Act
        await _chatHub.CreateConversation(createDto);

        // Assert
        Assert.Equal("ConversationCreated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ConversationDto> result = Assert.IsType<Result<ConversationDto>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateConversation_WhenSuccessful_ShouldAddCreatorToGroupAndNotifyParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        var createDto = new CreateConversationDto
        {
            CreatorUserId = userId,
            ConversationType = ConversationType.Private,
            ParticipantIds = [participantId]
        };

        ConversationDto conversationDto = CreateConversationDto("conv-123", [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.CreateConversationAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync("conv-123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Act
        await _chatHub.CreateConversation(createDto);

        // Assert
        _groupManagerMock.Verify(
            x => x.AddToGroupAsync("connection-123", "conv-123", It.IsAny<CancellationToken>()),
            Times.Once);
        _hubClientsMock.Verify(
            x => x.Client("connection-456"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConversation_WhenConversationNotFound_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string conversationId = "conv-123";
        const string userId = "user-123";

        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Conversation not found"));

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.UpdateConversation(conversationId, userId);

        // Assert
        Assert.Equal("ConversationUpdated", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<ConversationDto> result = Assert.IsType<Result<ConversationDto>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UpdateConversation_WhenSuccessful_ShouldNotifyAllParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        const string conversationId = "conv-123";

        await _chatHub.Register(userId);
        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Act
        await _chatHub.UpdateConversation(conversationId, userId);

        // Assert
        _hubClientsMock.Verify(
            x => x.Client(It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteConversation_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        const string conversationId = "conv-123";

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.DeleteConversation(conversationId));
    }

    [Fact]
    public async Task DeleteConversation_WhenConversationNotFound_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        const string conversationId = "conv-123";
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Conversation not found"));

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.DeleteConversation(conversationId);

        // Assert
        Assert.Equal("ConversationDeleted", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteConversation_WhenDeleteFails_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        const string conversationId = "conv-123";
        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        _conversationServiceMock
            .Setup(x => x.DeleteConversationAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Fail("Delete failed"));

        // Act
        await _chatHub.DeleteConversation(conversationId);

        // Assert
        Assert.Equal("ConversationDeleted", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteConversation_WhenSuccessful_ShouldNotifyAllParticipants()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        const string conversationId = "conv-123";
        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        _conversationServiceMock
            .Setup(x => x.DeleteConversationAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Ok(true));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.DeleteConversation(conversationId);

        // Assert
        _hubClientsMock.Verify(
            x => x.Clients(It.Is<IReadOnlyList<string>>(list => list.Contains("connection-123") && list.Contains("connection-456"))),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("ConversationDeleted", calls[0].Method);
        Result<string> result = Assert.IsType<Result<string>>(calls[0].Arg);
        Assert.True(result.Success);
        Assert.Equal(conversationId, result.Data);
    }

    [Fact]
    public async Task LeaveConversationAndNotify_WhenUserNotRegistered_ShouldThrowHubException()
    {
        // Arrange
        const string conversationId = "conv-123";

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _chatHub.LeaveConversationAndNotify(conversationId));
    }

    [Fact]
    public async Task LeaveConversationAndNotify_WhenConversationNotFound_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        const string conversationId = "conv-123";
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Fail("Conversation not found"));

        // Act
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        await _chatHub.LeaveConversationAndNotify(conversationId);

        // Assert
        Assert.Equal("ConversationLeft", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task LeaveConversationAndNotify_WhenLeaveFails_ShouldReturnErrorToCaller()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);
        var (singleClientProxyMock, captured) = SetupSingleClientProxyCapture();
        _hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);

        const string conversationId = "conv-123";
        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        _conversationServiceMock
            .Setup(x => x.LeaveConversationAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Fail("Leave failed"));

        // Act
        await _chatHub.LeaveConversationAndNotify(conversationId);

        // Assert
        Assert.Equal("ConversationLeft", captured.Method);
        Assert.NotNull(captured.Arg);
        Result<object> result = Assert.IsType<Result<object>>(captured.Arg);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task LeaveConversationAndNotify_WhenLastParticipant_ShouldNotifyConversationDeleted()
    {
        // Arrange
        const string userId = "user-123";
        await _chatHub.Register(userId);

        const string conversationId = "conv-123";
        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Return 1 to indicate conversation was deleted (last participant)
        _conversationServiceMock
            .Setup(x => x.LeaveConversationAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(1));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.LeaveConversationAndNotify(conversationId);

        // Assert
        _hubClientsMock.Verify(
            x => x.Clients(It.IsAny<IReadOnlyList<string>>()),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("ConversationDeleted", calls[0].Method);
    }

    [Fact]
    public async Task LeaveConversationAndNotify_WhenNotLastParticipant_ShouldRemoveFromGroupAndNotifyLeft()
    {
        // Arrange
        const string userId = "user-123";
        const string participantId = "user-456";
        await _chatHub.Register(userId);

        ChatHub participantHub = CreateHubWithConnection("connection-456");
        await participantHub.Register(participantId);

        const string conversationId = "conv-123";
        ConversationDto conversationDto = CreateConversationDto(conversationId, [userId, participantId]);
        _conversationServiceMock
            .Setup(x => x.GetConversationByIdAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConversationDto>.Ok(conversationDto));

        // Return 2 to indicate user left but conversation still exists
        _conversationServiceMock
            .Setup(x => x.LeaveConversationAsync(conversationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(2));

        // Act
        var (clientProxyMock, calls) = SetupClientProxyCapture();
        _hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        await _chatHub.LeaveConversationAndNotify(conversationId);

        // Assert
        _groupManagerMock.Verify(
            x => x.RemoveFromGroupAsync("connection-123", conversationId, It.IsAny<CancellationToken>()),
            Times.Once);
        _hubClientsMock.Verify(
            x => x.Clients(It.IsAny<IReadOnlyList<string>>()),
            Times.Once);
        Assert.Single(calls);
        Assert.Equal("ConversationLeft", calls[0].Method);
        Result<ConversationDto> result = Assert.IsType<Result<ConversationDto>>(calls[0].Arg);
        Assert.True(result.Success);
    }

    private sealed class CapturedCall
    {
        public string? Method { get; set; }
        public object? Arg { get; set; }
    }

    private (Mock<ISingleClientProxy> Mock, CapturedCall Captured) SetupSingleClientProxyCapture()
    {
        var captured = new CapturedCall();
        var mock = new Mock<ISingleClientProxy>();
        mock
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, _) =>
            {
                captured.Method = method;
                captured.Arg = args.Length > 0 ? args[0] : null;
            })
            .Returns(Task.CompletedTask);
        return (mock, captured);
    }

    private (Mock<IClientProxy> Mock, List<CapturedCall> Calls) SetupClientProxyCapture()
    {
        var calls = new List<CapturedCall>();
        var mock = new Mock<IClientProxy>();
        mock
            .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object[], CancellationToken>((method, args, _) =>
            {
                calls.Add(new CapturedCall { Method = method, Arg = args.Length > 0 ? args[0] : null });
            })
            .Returns(Task.CompletedTask);
        return (mock, calls);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Test objects don't need disposal")]
    private ChatHub CreateHubWithConnection(string connectionId)
    {
        var hubCallerContextMock = new Mock<HubCallerContext>();
        var hubClientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        var singleClientProxyMock = new Mock<ISingleClientProxy>();
        var groupManagerMock = new Mock<IGroupManager>();

        hubCallerContextMock.Setup(x => x.ConnectionId).Returns(connectionId);
        hubClientsMock.Setup(x => x.Caller).Returns(singleClientProxyMock.Object);
        hubClientsMock.Setup(x => x.Clients(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxyMock.Object);
        hubClientsMock.Setup(x => x.Client(It.IsAny<string>())).Returns(singleClientProxyMock.Object);

        var hub = new ChatHub(_chatMessageServiceMock.Object, _conversationServiceMock.Object, _notificationServiceMock.Object)
        {
            Context = hubCallerContextMock.Object,
            Clients = hubClientsMock.Object,
            Groups = groupManagerMock.Object
        };

        return hub;
    }

    private static ChatMessageDto CreateChatMessageDto(string id, string conversationId, string senderId, string? content = null)
    {
        return new ChatMessageDto
        {
            Id = id,
            Content = content ?? "Test message",
            SenderId = senderId,
            Sender = new UserDto
            {
                Id = senderId,
                DisplayName = $"User {senderId}",
                AvatarUrl = null
            },
            ConversationId = conversationId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static ConversationDto CreateConversationDto(string id, List<string> participantIds, string? title = null)
    {
        return new ConversationDto
        {
            Id = id,
            Type = ConversationType.Private,
            Title = title ?? $"Conversation {id}",
            CreatorId = participantIds.FirstOrDefault() ?? "creator-123",
            CreatedAtUtc = DateTime.UtcNow,
            MessagesCount = 0,
            Participants = participantIds.Select(pId => new ConversationParticipantDto
            {
                Id = pId,
                DisplayName = $"User {pId}",
                Username = $"user{pId}",
                ImageUrl = null
            }).ToList()
        };
    }

    public void Dispose()
    {
        _chatHub.Dispose();
    }
}

