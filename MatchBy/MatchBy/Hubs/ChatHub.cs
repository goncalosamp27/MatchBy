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
            ? uid : throw new HubException("Ligação não registada.");
    
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
            throw new HubException("Remetente inválido.");
        }
        
        ChatMessageDto? newMsg = await chatMessageService.CreateChatMessageAsync(createChatMessageDto);
        
        if(newMsg is null)
        {
            throw new HubException("Não foi possível criar a mensagem.");
        }
        
        ConversationDto? conv = await conversationService.GetConversationByIdAsync(newMsg.ConversationId, userId);
        
        if (conv is null)
        {
            throw new HubException("Conversa não encontrada.");
        }
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        await Clients.Clients(participantConnections)
            .SendAsync("MessageCreated", newMsg);
    }

    public async Task UpdateMessage(UpdateChatMessageDto updateChatMessageDto)
    {
        string userId = EnsureUser();
        if (updateChatMessageDto.CreatorUserId != userId)
        {
            throw new HubException("Remetente inválido.");
        }

        ChatMessageDto? newMsg = await chatMessageService.UpdateChatMessageAsync(updateChatMessageDto);
        
        if(newMsg is null)
        {
            throw new HubException("Não foi possível criar a mensagem.");
        }

        ConversationDto? conv = await conversationService.GetConversationByIdAsync(newMsg.ConversationId, userId);
        
        if (conv is null)
        {
            throw new HubException("Conversa não encontrada.");
        }
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        await Clients.Clients(participantConnections)
            .SendAsync("MessageUpdated", newMsg);
    }

    public async Task DeleteMessage(string chatMessageId)
    {
        string userId = EnsureUser();
        
        ChatMessageDto? msg = await chatMessageService.GetChatMessageByIdAsync(chatMessageId, userId);

        if (msg is null)
        {
            throw new HubException("Mensagem não encontrada.");
        }
        
        bool ok = await chatMessageService.DeleteChatMessageAsync(chatMessageId, userId);
        if (!ok)
        {
            throw new HubException("Não foi possível apagar.");
        }
        
        ConversationDto? conv = await conversationService.GetConversationByIdAsync(msg.ConversationId, userId);
        
        if (conv is null)
        {
            throw new HubException("Conversa não encontrada.");
        }
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        await Clients.Clients(participantConnections)
            .SendAsync("MessageDeleted", conv, chatMessageId);
    }

    private static string Group(string conversationId) => conversationId;
    
    public async Task CreateConversation(CreateConversationDto dto)
    {
        string userId = EnsureUser();
        if (dto.CreatorUserId != userId)
        {
            throw new HubException("Criador inválido.");
        }

        ConversationDto? conv = await conversationService.CreateConversationAsync(dto);
        if (conv is null)
        {
            throw new HubException("Não foi possível criar a conversa.");
        }

        // Adiciona o criador ao grupo
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(conv.Id));
        
        // Notifica todos os participantes conectados
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        foreach (string participant in participantConnections)
        {
            ConversationDto? conversation = await conversationService.GetConversationByIdAsync(conv.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationCreated", conversation);
        }
    }
    
    public async Task UpdateConversation(UpdateConversationDto dto)
    {
        string userId = EnsureUser();
        if (dto.CreatorUserId != userId)
        {
            throw new HubException("Criador inválido.");
        }
        
        ConversationDto? conv = await conversationService.UpdateConversationAsync(dto);
        if (conv is null)
        {
            throw new HubException("Não foi possível atualizar a conversa.");
        }

        // Notifica todos os participantes conectados
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        foreach (string participant in participantConnections)
        {
            ConversationDto? conversation = await conversationService.GetConversationByIdAsync(conv.Id, ConnectionUsers[participant]);
            await Clients.Client(participant)
                .SendAsync("ConversationCreated", conversation);
        }
    }
    
    public async Task DeleteConversation(string conversationId)
    {
        string userId = EnsureUser();
        
        // Obtém a conversa antes de apagar para ter acesso aos participantes
        ConversationDto? conv = await conversationService.GetConversationByIdAsync(conversationId, userId);
        if (conv is null)
        {
            throw new HubException("Conversa não encontrada.");
        }
        
        if (!await conversationService.DeleteConversationAsync(conversationId, userId))
        {
            throw new HubException("Não foi possível apagar.");
        }

        // Notifica todos os participantes conectados
        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        await Clients.Clients(participantConnections)
            .SendAsync("ConversationDeleted", conversationId);
    }
    
    public async Task LeaveConversationAndNotify(string conversationId)
    {
        string userId = EnsureUser();
        
        // Obtém a conversa antes de sair para ter acesso aos participantes
        ConversationDto? conv = await conversationService.GetConversationByIdAsync(conversationId, userId);
        if (conv is null)
        {
            throw new HubException("Conversa não encontrada.");
        }

        var participantConnections = GetParticipantsConnections(conv.Participants).ToList();
        
        int result = await conversationService.LeaveConversationAsync(conversationId, userId);
        switch (result)
        {
            case 0:
                throw new HubException("Não foi possível sair.");
            case 1:
                {
                    await Clients.Clients(participantConnections)
                        .SendAsync("ConversationDeleted", conversationId);
                    break;
                }
            case 2:
                // Remove todas as conexões do utilizador do grupo
                IEnumerable<string> userConnections = GetUserConnections(userId);
                foreach (string connectionId in userConnections)
                {
                    await Groups.RemoveFromGroupAsync(connectionId, Group(conversationId));
                }
                await Clients.Clients(participantConnections)
                    .SendAsync("ConversationLeft", conv);
                break;
                
        }
    }
}
