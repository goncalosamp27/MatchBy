using System.Collections.Concurrent;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using MatchBy.Services.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace MatchBy.Hubs;

public class ChatHub(
    IChatMessageService chatMessageService, 
    IConversationService conversationService,
    INotificationService notificationService) : Hub
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

    /// <summary>
    /// Handles client disconnection by removing the connection from user tracking dictionaries.
    /// </summary>
    /// <param name="exception">Optional exception that caused the disconnection.</param>
    /// <returns>
    /// A task representing the asynchronous disconnection operation.
    /// </returns>
    /// <remarks>
    /// This method removes the connection from both the connection-to-user mapping and the user-to-connections mapping.
    /// If a user has no remaining connections, their entry is removed from the UserConnections dictionary.
    /// </remarks>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (!ConnectionUsers.TryRemove(Context.ConnectionId, out string? userId) ||
            !UserConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            return base.OnDisconnectedAsync(exception);
        }

        lock (connections)
        {
            connections.Remove(Context.ConnectionId);
            if (connections.Count == 0)
            {
                UserConnections.TryRemove(userId, out _);
            }
        }

        return base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Registers a client connection with a specific user ID for chat message routing.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to register this connection for.</param>
    /// <returns>
    /// A task representing the asynchronous registration operation. Sends a "Registered" message to the caller.
    /// </returns>
    /// <remarks>
    /// This method associates the current SignalR connection with a user ID, allowing chat messages
    /// to be routed to all active connections for that user. Multiple connections per user are supported.
    /// </remarks>
    public async Task Register(string userId)
    {
        ConnectionUsers[Context.ConnectionId] = userId;

        UserConnections.AddOrUpdate(
            userId,
            _ => [Context.ConnectionId],
            (_, connections) =>
            {
                lock (connections)
                {
                    connections.Add(Context.ConnectionId);
                }

                return connections;
            });

        await Clients.Caller.SendAsync("Registered", new { userId });
    }

    /// <summary>
    /// Ensures that the current connection is registered with a user ID.
    /// </summary>
    /// <returns>
    /// The user ID associated with the current connection.
    /// </returns>
    /// <exception cref="HubException">Thrown when the connection is not registered with a user ID.</exception>
    private string EnsureUser()
        => ConnectionUsers.TryGetValue(Context.ConnectionId, out string? uid)
            ? uid
            : throw new HubException("Connection not registered.");

    /// <summary>
    /// Gets all active SignalR connection IDs for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>
    /// A collection of connection IDs for the user, or an empty collection if the user has no active connections.
    /// </returns>
    /// <remarks>
    /// This method returns a thread-safe snapshot of the user's connections.
    /// </remarks>
    private IEnumerable<string> GetUserConnections(string userId)
    {
        if (!UserConnections.TryGetValue(userId, out HashSet<string>? connections))
        {
            return [];
        }

        lock (connections)
        {
            return [.. connections];
        }
    }

    /// <summary>
    /// Gets all active SignalR connection IDs for a list of conversation participants.
    /// </summary>
    /// <param name="participants">List of conversation participants to get connections for.</param>
    /// <returns>
    /// A collection of unique connection IDs for all participants, or an empty collection if no participants have active connections.
    /// </returns>
    private IEnumerable<string> GetParticipantsConnections(List<ConversationParticipantDto> participants)
    {
        return participants.SelectMany(p => GetUserConnections(p.Id)).Distinct();
    }

    //--------------------- Conversation Methods -----------------

    /// <summary>
    /// Adds the current client connection to a conversation group for receiving real-time updates.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to join.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// Once joined to a conversation group, the client will receive all real-time updates (messages, updates, deletions)
    /// for that conversation through SignalR group messaging.
    /// </remarks>
    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conversationId));
    }

    /// <summary>
    /// Removes the current client connection from a conversation group.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to leave.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <remarks>
    /// After leaving a conversation group, the client will no longer receive real-time updates for that conversation.
    /// </remarks>
    public async Task LeaveConversation(string conversationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(conversationId));

    /// <summary>
    /// Creates a new chat message and broadcasts it to all conversation participants.
    /// </summary>
    /// <param name="createChatMessageDto">DTO containing the message creation details (content, conversation ID, reply to message, etc.).</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "MessageCreated" event to all conversation participants.
    /// </returns>
    /// <remarks>
    /// This method validates the sender, creates the message via the chat message service, and broadcasts it to all
    /// active connections of conversation participants. If the message is a reply, a notification is sent to the
    /// original message sender (if they are not the current sender).
    /// </remarks>
    public async Task CreateMessage(CreateChatMessageDto createChatMessageDto)
    {
        string userId = EnsureUser();
        if (createChatMessageDto.CreatorUserId != userId)
        {
            await Clients.Caller.SendAsync("MessageCreated",
                Result<ChatMessageDto>.Fail("Invalid sender."));
            return;
        }

        Result<ChatMessageDto> newMsg = await chatMessageService.CreateChatMessageAsync(createChatMessageDto);

        if (!newMsg.Success)
        {
            // Notify only the caller of the error
            await Clients.Caller.SendAsync("MessageCreated", newMsg);
            return;
        }

        Result<ConversationDto> conv =
            await conversationService.GetConversationByIdAsync(newMsg.Data.ConversationId, userId);

        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("MessageCreated",
                Result<ChatMessageDto>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("MessageCreated", newMsg);

        // Send notification if this is a reply to someone's message
        if (!string.IsNullOrEmpty(newMsg.Data.ReplyToMessageId) && 
            newMsg.Data.ReplyToMessage != null && 
            newMsg.Data.ReplyToMessage.SenderId != userId)
        {
            string senderName = newMsg.Data.Sender.DisplayName;
            string conversationTitle = conv.Data.Title ?? "conversation";
            
            var replyNotification = new CreateNotificationDto
            {
                Type = NotificationType.Message,
                ReceiverUserId = newMsg.Data.ReplyToMessage.SenderId,
                SenderUserId = userId,
                RelatedEntityId = newMsg.Data.ConversationId,
                RelatedEntityName = conversationTitle,
                Title = "New reply",
                Message = $"{senderName} replied to your message in {conversationTitle}",
                ActionUrl = $"/chat/{newMsg.Data.ConversationId}"
            };

            await notificationService.SendNotificationAsync(replyNotification, CancellationToken.None);
        }
    }

    /// <summary>
    /// Updates an existing chat message and broadcasts the update to all conversation participants.
    /// </summary>
    /// <param name="updateChatMessageDto">DTO containing the message update details (message ID, new content, creator user ID).</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "MessageUpdated" event to all conversation participants.
    /// </returns>
    /// <remarks>
    /// This method validates the sender, updates the message via the chat message service, and broadcasts the update
    /// to all active connections of conversation participants. Only the message sender can update their own messages.
    /// </remarks>
    public async Task UpdateMessage(UpdateChatMessageDto updateChatMessageDto)
    {
        try
        {
            string userId = EnsureUser();
            if (updateChatMessageDto.CreatorUserId != userId)
            {
                await Clients.Caller.SendAsync("MessageUpdated",
                    Result<ChatMessageDto>.Fail("Invalid sender."));
                return;
            }

            Result<ChatMessageDto> updatedMsg = await chatMessageService.UpdateChatMessageAsync(updateChatMessageDto);

            if (!updatedMsg.Success)
            {
                await Clients.Caller.SendAsync("MessageUpdated", updatedMsg);
                return;
            }

            Result<ConversationDto> conv =
                await conversationService.GetConversationByIdAsync(updatedMsg.Data.ConversationId, userId);

            if (!conv.Success)
            {
                await Clients.Caller.SendAsync("MessageUpdated",
                    Result<ChatMessageDto>.Fail("Conversation not found."));
                return;
            }

            var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

            await Clients.Clients(participantConnections)
                .SendAsync("MessageUpdated", updatedMsg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating message: {ex.Message}");
            await Clients.Caller.SendAsync("MessageUpdated",
                Result<ChatMessageDto>.Fail("An error occurred while updating the message."));
        }
    }

    public record MessageDeletedDto(ConversationDto Conversation, string MessageId);

    /// <summary>
    /// Deletes a chat message and broadcasts the deletion to all conversation participants.
    /// </summary>
    /// <param name="chatMessageId">The unique identifier of the message to delete.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "MessageDeleted" event with conversation and message ID to all conversation participants.
    /// </returns>
    /// <remarks>
    /// This method validates the sender, deletes the message via the chat message service, and broadcasts the deletion
    /// to all active connections of conversation participants. Only the message sender can delete their own messages.
    /// </remarks>
    public async Task DeleteMessage(string chatMessageId)
    {
        string userId = EnsureUser();

        Result<ChatMessageDto> msg = await chatMessageService.GetChatMessageByIdAsync(chatMessageId, userId);
        if (!msg.Success)
        {
            await Clients.Caller.SendAsync("MessageDeleted",
                Result<object>.Fail("Message not found."));
            return;
        }

        Result<bool> deleteResult = await chatMessageService.DeleteChatMessageAsync(chatMessageId, userId);
        if (!deleteResult.Success)
        {
            await Clients.Caller.SendAsync("MessageDeleted",
                Result<object>.Fail([.. deleteResult.ErrorMessages]));
            return;
        }

        Result<ConversationDto> conv =
            await conversationService.GetConversationByIdAsync(msg.Data.ConversationId, userId);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("MessageDeleted",
                Result<object>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("MessageDeleted", Result<MessageDeletedDto>.Ok(new MessageDeletedDto(
                conv.Data,
                chatMessageId
            )));
    }

    /// <summary>
    /// Generates a SignalR group name for a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <returns>
    /// The group name (currently the conversation ID itself).
    /// </returns>
    private static string Group(string conversationId) => conversationId;

    /// <summary>
    /// Creates a new conversation and notifies all participants.
    /// </summary>
    /// <param name="dto">DTO containing conversation creation details (type, title, participants, team).</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "ConversationCreated" event to all participants.
    /// </returns>
    /// <remarks>
    /// This method validates the creator, creates the conversation via the conversation service, adds the creator
    /// to the conversation group, and notifies all participants (including the creator) via their active connections.
    /// </remarks>
    public async Task CreateConversation(CreateConversationDto dto)
    {
        string userId = EnsureUser();
        if (dto.CreatorUserId != userId)
        {
            await Clients.Caller.SendAsync("ConversationCreated",
                Result<ConversationDto>.Fail("Invalid creator."));
            return;
        }

        Result<ConversationDto> conv = await conversationService.CreateConversationAsync(dto);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("ConversationCreated", conv);
            return;
        }

        // Add creator to the group
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conv.Data.Id));

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        foreach (string participant in participantConnections)
        {
            Result<ConversationDto> conversation =
                await conversationService.GetConversationByIdAsync(conv.Data.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationCreated", conversation);
        }
    }

    /// <summary>
    /// Updates a conversation and broadcasts the update to all participants.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to update.</param>
    /// <param name="userId">The unique identifier of the user requesting the update.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "ConversationUpdated" event to all conversation participants.
    /// </returns>
    /// <remarks>
    /// This method retrieves the updated conversation and broadcasts it to all active connections of conversation participants.
    /// Each participant receives the conversation data formatted for their perspective.
    /// </remarks>
    public async Task UpdateConversation(string conversationId, string userId)
    {
        Result<ConversationDto> conv = await conversationService.GetConversationByIdAsync(conversationId, userId);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("ConversationUpdated", conv);
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        foreach (string participant in participantConnections)
        {
            Result<ConversationDto> conversation =
                await conversationService.GetConversationByIdAsync(conv.Data.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationUpdated", conversation);
        }
    }

    /// <summary>
    /// Deletes a conversation and notifies all participants.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to delete.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends "ConversationDeleted" event with the conversation ID to all participants.
    /// </returns>
    /// <remarks>
    /// This method retrieves the conversation participants before deletion, deletes the conversation via the conversation service,
    /// and notifies all participants via their active connections. Only users with permission can delete conversations.
    /// </remarks>
    public async Task DeleteConversation(string conversationId)
    {
        string userId = EnsureUser();

        // Get conversation before deleting to access participants
        Result<ConversationDto> conv = await conversationService.GetConversationByIdAsync(conversationId, userId);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("ConversationDeleted",
                Result<object>.Fail("Conversation not found."));
            return;
        }

        Result<bool> deleteResult = await conversationService.DeleteConversationAsync(conversationId, userId);
        if (!deleteResult.Success)
        {
            await Clients.Caller.SendAsync("ConversationDeleted",
                Result<object>.Fail(deleteResult.ErrorMessages.ToArray()));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("ConversationDeleted", Result<string>.Ok(conversationId));
    }

    /// <summary>
    /// Removes a user from a conversation and notifies all participants. If the user is the last participant, the conversation is deleted.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to leave.</param>
    /// <returns>
    /// A task representing the asynchronous operation. Sends either "ConversationDeleted" or "ConversationLeft" event to all participants.
    /// </returns>
    /// <remarks>
    /// This method retrieves the conversation participants before leaving, removes the user via the conversation service,
    /// removes all user connections from the conversation group, and notifies all participants. If the conversation is deleted
    /// (last participant left), a "ConversationDeleted" event is sent. Otherwise, a "ConversationLeft" event is sent with the updated conversation.
    /// </remarks>
    public async Task LeaveConversationAndNotify(string conversationId)
    {
        string userId = EnsureUser();

        // Get conversation before leaving to access participants
        Result<ConversationDto> conv = await conversationService.GetConversationByIdAsync(conversationId, userId);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("ConversationLeft",
                Result<object>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        Result<int> leaveResult = await conversationService.LeaveConversationAsync(conversationId, userId);

        if (!leaveResult.Success)
        {
            await Clients.Caller.SendAsync("ConversationLeft",
                Result<object>.Fail(leaveResult.ErrorMessages.ToArray()));
            return;
        }

        switch (leaveResult.Data)
        {
            case 1:
                {
                    // Conversation was deleted (last participant left)
                    await Clients.Clients(participantConnections)
                        .SendAsync("ConversationDeleted", Result<object>.Ok(new
                        {
                            ConversationId = conversationId
                        }));
                    break;
                }
            case 2:
                // User left but conversation still exists
                // Remove all user connections from the group
                IEnumerable<string> userConnections = GetUserConnections(userId);
                foreach (string connectionId in userConnections)
                {
                    await Groups.RemoveFromGroupAsync(connectionId, Group(conversationId));
                }

                await Clients.Clients(participantConnections)
                    .SendAsync("ConversationLeft", Result<ConversationDto>.Ok(conv.Data));
                break;
        }
    }
}
