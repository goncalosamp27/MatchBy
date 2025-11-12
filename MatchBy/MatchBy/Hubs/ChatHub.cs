using System.Collections.Concurrent;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;
using MatchBy.Services.ChatMessages;
using MatchBy.Services.Conversations;
using Microsoft.AspNetCore.SignalR;

namespace MatchBy.Hubs;

public class ChatHub(IChatMessageService chatMessageService, IConversationService conversationService) : Hub
{
    // Mapeia userId -> lista de connectionIds
    private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();

    // Mapeia connectionId -> userId (para lookup rápido)
    private static readonly ConcurrentDictionary<string, string> ConnectionUsers = new();

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

    private string EnsureUser()
        => ConnectionUsers.TryGetValue(Context.ConnectionId, out string? uid)
            ? uid
            : throw new HubException("Ligação não registada.");

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

    private IEnumerable<string> GetParticipantsConnections(List<ConversationParticipantDto> participants)
    {
        return participants.SelectMany(p => GetUserConnections(p.Id)).Distinct();
    }

    //--------------------- Conversation Methods -----------------

    public async Task JoinConversation(string conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conversationId));
    }

    public async Task LeaveConversation(string conversationId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(conversationId));

    public async Task CreateMessage(CreateChatMessageDto createChatMessageDto)
    {
        string userId = EnsureUser();
        if (createChatMessageDto.CreatorUserId != userId)
        {
            // Instead of throw, return error to caller
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
            await conversationService.GetConversationByIdAsync(newMsg.Data!.ConversationId, userId);

        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("MessageCreated",
                Result<ChatMessageDto>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("MessageCreated", newMsg);
    }

    public async Task UpdateMessage(UpdateChatMessageDto updateChatMessageDto)
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
            await conversationService.GetConversationByIdAsync(updatedMsg.Data!.ConversationId, userId);

        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("MessageUpdated",
                Result<ChatMessageDto>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("MessageUpdated", updatedMsg);
    }

    public record MessageDeletedDto(ConversationDto Conversation, string MessageId);

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
            await conversationService.GetConversationByIdAsync(msg.Data!.ConversationId, userId);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("MessageDeleted",
                Result<object>.Fail("Conversation not found."));
            return;
        }

        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

        // Send success to all participants
        await Clients.Clients(participantConnections)
            .SendAsync("MessageDeleted", Result<MessageDeletedDto>.Ok(new MessageDeletedDto(
                conv.Data!,
                chatMessageId
            )));
    }

    private static string Group(string conversationId) => conversationId;

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
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conv.Data!.Id));

        // Notify all connected participants
        var participantConnections = GetParticipantsConnections(conv.Data.Participants).ToList();

        foreach (string participant in participantConnections)
        {
            Result<ConversationDto> conversation =
                await conversationService.GetConversationByIdAsync(conv.Data.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationCreated", conversation);
        }
    }

    public async Task UpdateConversation(UpdateConversationDto dto)
    {
        string userId = EnsureUser();
        if (dto.CreatorUserId != userId)
        {
            await Clients.Caller.SendAsync("ConversationUpdated",
                Result<ConversationDto>.Fail("Invalid creator."));
            return;
        }

        Result<ConversationDto> conv = await conversationService.UpdateConversationAsync(dto);
        if (!conv.Success)
        {
            await Clients.Caller.SendAsync("ConversationUpdated", conv);
            return;
        }

        // Notify all connected participants
        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

        foreach (string participant in participantConnections)
        {
            Result<ConversationDto> conversation =
                await conversationService.GetConversationByIdAsync(conv.Data.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationCreated", conversation);
        }
    }

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

        // Notify all connected participants
        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

        await Clients.Clients(participantConnections)
            .SendAsync("ConversationDeleted", Result<string>.Ok(conversationId));
    }

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

        var participantConnections = GetParticipantsConnections(conv.Data!.Participants).ToList();

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
                    .SendAsync("ConversationLeft", Result<ConversationDto>.Ok(conv.Data!));
                break;
        }
    }
}
