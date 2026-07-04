using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.ChatMessage;

public class ChatMessageRepository: IChatMessageRepository
{
    private static IQueryable<Models.ChatMessage> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage)
            .ThenInclude(r => r!.Sender)
            .Include(m => m.Conversation);
    }
    public async Task<CursorPaginationResponse<List<Models.ChatMessage>>> GetChatMessagesAsync(string conversationId, string userId, ApplicationDbContext dbContext, int pageSize, string? cursor,
        CancellationToken ct = default)
    {
        IQueryable<Models.ChatMessage> query = Query(dbContext)
            .Where(m => m.ConversationId == conversationId);

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(m => m.Id.CompareTo(cursor) < 0);
        }

        List<Models.ChatMessage> messages = await query
            .OrderByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(ct);

        bool hasNextPage = messages.Count > pageSize;

        if (hasNextPage)
        {
            messages.RemoveAt(messages.Count - 1);
        }
        
        messages.Reverse();

        string? nextCursor = hasNextPage && messages.Any()
            ? messages[0].Id
            : null;
    
        return new CursorPaginationResponse<List<Models.ChatMessage>>
        {
            Data = messages,
            NextCursor = nextCursor
        };
    }

    public async Task<Models.ChatMessage?> GetByIdAsync(string chatMessageId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext)
            .Where(m => m.Id == chatMessageId)
            .FirstOrDefaultAsync(ct);
    }

    public void Add(Models.ChatMessage entity, ApplicationDbContext dbContext)
    {
        dbContext.ChatMessages.Add(entity);
    }

    public void Update(Models.ChatMessage entity, ApplicationDbContext dbContext)
    {
        dbContext.ChatMessages.Update(entity);
    }

    public void Remove(Models.ChatMessage entity, ApplicationDbContext dbContext)
    {
        dbContext.ChatMessages.Remove(entity);
    }
}