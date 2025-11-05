using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using ChatMessage = MatchBy.Models.ChatMessage;

namespace MatchBy.Services;

public class ChatMessageService(ApplicationDbContext applicationDbContext): IChatMessageService
{
    public async Task<List<ChatMessage>> GetChatMessagesAsync(string conversationId, string userId, CancellationToken ct = default)
    {
        bool isUserInvolved = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .Where(c => c.CreatorId == userId || c.Participants.Any(p => p.Id == userId))
            .AnyAsync(ct);

        if (!isUserInvolved)
        {
            return [];
        }
        
        List<ChatMessage> chatMessages = await applicationDbContext
            .ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .Include(m => m.Conversation)
            .Where(m => m.ConversationId == conversationId)
            .AsNoTracking()
            .ToListAsync(ct);
        
        return chatMessages;
    }

    public async Task<ChatMessage?> GetChatMessageByIdAsync(string chatMessageId, string userId, CancellationToken ct = default)
    {
        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .Include(m => m.Conversation)
            .Where(m => m.Id.Equals(chatMessageId))
            .FirstOrDefaultAsync(ct);
        
        if (chatMessage is null)
        {
            return null;
        }
        
        bool isUserInvolved = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Where(c => c.CreatorId == userId || c.Participants.Any(p => p.Id == userId))
            .AnyAsync(ct);
        
        return isUserInvolved ? chatMessage : null;
    }

    public async Task<ChatMessage?> CreateChatMessageAsync(string content, string creatorUserId, string conversationId, string? replyToMessageId = null,
        CancellationToken ct = default)
    {
        ApplicationUser? sender = await applicationDbContext.Users
            .Where(u => u.Id == creatorUserId)
            .FirstOrDefaultAsync(ct);
        if (sender is null)
        {
            return null;
        }
        
        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId)
            .Where(c => c.CreatorId == creatorUserId || c.Participants.Any(p => p.Id == creatorUserId))
            .FirstOrDefaultAsync(ct);
        if (conversation is null)
        {
            return null;
        }
        
        conversation.LastMessageAtUtc = DateTime.UtcNow;
        
        var chatMessage = new ChatMessage
        {
            Id = $"chatMessage_{Guid.CreateVersion7()}",
            Content = content,
            SenderId = creatorUserId,
            ReplyToMessageId = replyToMessageId,
            ConversationId = conversationId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };
        
        await applicationDbContext.ChatMessages.AddAsync(chatMessage, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        
        chatMessage.Sender = sender;
        if (replyToMessageId == null)
        {
            return chatMessage;
        }

        ChatMessage? replyToMessage = await applicationDbContext.ChatMessages
            .Where(m => m.Id == replyToMessageId)
            .FirstOrDefaultAsync(ct);
            
        chatMessage.ReplyToMessage = replyToMessage;
        return chatMessage;
    }

    public async Task<ChatMessage?> UpdateChatMessageAsync(string chatMessageId, string content, string creatorUserId,
        CancellationToken ct = default)
    {
        ApplicationUser? sender = await applicationDbContext.Users
            .Where(u => u.Id == creatorUserId)
            .FirstOrDefaultAsync(ct);
        if (sender is null)
        {
            return null;
        }
        
        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Where(m => m.Id == chatMessageId && m.DeletedAtUtc == null)
            .Where(m => m.SenderId == creatorUserId )
            .Include(m => m.ReplyToMessage)
            .Include(m => m.Conversation)
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(ct);

        if (chatMessage is null)
        {
            return null;
        }
        
        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);
        
        if (conversation is null)
        {
            return null;
        }

        conversation.LastMessageAtUtc = DateTime.UtcNow;

        chatMessage.Content = content;
        chatMessage.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);
            
        return chatMessage;
    }

    public async Task<bool> DeleteChatMessageAsync(string chatMessageId, string userId, CancellationToken ct = default)
    {
        ChatMessage? chatMessage = await applicationDbContext.ChatMessages
            .Where(c => c.Id == chatMessageId && c.DeletedAtUtc == null)
            .Where(c => c.SenderId == userId)
            .FirstOrDefaultAsync(ct);
        
        if (chatMessage is null)
        {
            return false;
        }
        
        Conversation? conversation = await applicationDbContext.Conversations
            .Where(c => c.Id == chatMessage.ConversationId)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(ct);
        
        if (conversation is null)
        {
            return false;
        }

        //we have a query filter for DeletedAtUtc == null, so Messages will not include deleted messages
        conversation.LastMessageAtUtc = conversation.Messages.Count == 1 ? null : conversation.Messages.Last(m => m.Id != chatMessageId).CreatedAtUtc;

        // only the sender can delete their message
        bool canDelete = await applicationDbContext.ChatMessages
            .AnyAsync(c => c.Id == chatMessageId && c.SenderId == userId && c.DeletedAtUtc == null, ct);

        if (!canDelete)
        {
            return false;
        }

        int affected = await applicationDbContext.ChatMessages
            .Where(c => c.Id == chatMessageId && c.DeletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        await applicationDbContext.SaveChangesAsync(ct);
        return affected == 1;
    }
}

