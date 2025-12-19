using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.ChatConversation;

internal sealed class ConversationRepository : IConversationRepository
{
    /// <summary>
    /// Paginates conversations using cursor-based pagination with optional search filtering.
    /// </summary>
    /// <param name="baseQuery">The base queryable collection of conversations to paginate.</param>
    /// <param name="pageSize">The number of conversations to retrieve per page.</param>
    /// <param name="cursorReceived">Optional cursor for pagination. If provided, returns conversations before this cursor.</param>
    /// <param name="query">Optional search query to filter conversations by title or participant names (case-insensitive).</param>
    /// <returns>
    /// A cursor-paginated response with a list of conversation DTOs, ordered by last message date or creation date (newest first).
    /// </returns>
    /// <remarks>
    /// Conversations are ordered by last message date (if available) or creation date, then by ID.
    /// Images are refreshed in parallel for all conversations before returning.
    /// </remarks>
    private static async Task<CursorPaginationResponse<List<Conversation>>> PaginateAsync(
        IQueryable<Conversation> baseQuery,
        int pageSize,
        string? cursorReceived,
        string? query)
    {
        if (!string.IsNullOrWhiteSpace(cursorReceived))
        {
            var cursor = ConversationCursorDto.Decode(cursorReceived);
            if (cursor != null)
            {
                baseQuery = baseQuery
                    .Where(p =>
                        p.LastMessageAtUtc != null
                            ? p.LastMessageAtUtc < cursor.Date ||
                              p.LastMessageAtUtc == cursor.Date &&
                              string.Compare(p.Id, cursor.Id) <= 0
                            : p.CreatedAtUtc < cursor.Date ||
                              p.CreatedAtUtc == cursor.Date &&
                              string.Compare(p.Id, cursor.Id) <= 0);
            }
        }

        if (query != null)
        {
            baseQuery = baseQuery.Where(c => c.Title != null && c.Title.ToLower().Contains(query.ToLower()) ||
                                             c.Participants.Any(p => p.DisplayName.ToLower().Contains(query.ToLower())));
        }

        List<Conversation> items = await baseQuery
            .OrderByDescending(p => p.LastMessageAtUtc != null)
            .ThenByDescending(p => p.LastMessageAtUtc ?? p.CreatedAtUtc) 
            .ThenByDescending(p => p.Id)
            .Take(pageSize + 1)
            .ToListAsync();

        bool hasNext = items.Count > pageSize;
        string? nextCursor = null;
        if (hasNext)
        {
            Conversation last = items[^1];
            DateTime orderingDate = last.LastMessageAtUtc ?? last.CreatedAtUtc;
            nextCursor = ConversationCursorDto.Encode(last.Id, orderingDate);
            items.RemoveAt(items.Count - 1);
        }
        
        return new CursorPaginationResponse<List<Conversation>>
        {
            Data = items,
            NextCursor = nextCursor
        };
    }

    public async Task<bool> PrivateConversationExistsAsync(IReadOnlyCollection<string> participantIds, ApplicationDbContext dbContext, CancellationToken ct)
    {
        return await dbContext.Conversations
            .Include(c => c.Participants)
            .Where(c => c.Type == ConversationType.Private)
            .AnyAsync(c => c.Participants.Count == participantIds.Count &&
                           c.Participants.All(p =>participantIds.Contains(p.Id)), ct);
    }

    
    public async Task<Conversation?> GetByIdAsync(string conversationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        Conversation? conversation = await dbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .Include(m => m.Team)
            .Include(m => m.Messages)
            .Where(m => m.Id ==conversationId)
            .Where(m => m.Participants.Any(p => p.Id == userId))
            .FirstOrDefaultAsync(i => i.Id == conversationId, ct);
        if(conversation == null)
        {
            return null;
        }
        
        // For private conversations, set the title to the other participant's name
        if (conversation.Type == ConversationType.Private)
        {
            conversation.Title = conversation.Participants.FirstOrDefault(p => p.Id != userId)?.DisplayName;
        }
        return conversation;
    }

    public async Task<bool> IsParticipantAsync(string conversationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await dbContext.Conversations
            .Include(c => c.Creator)
            .Include(c => c.Team)
            .Include(c => c.Match)
            .AnyAsync(c => c.Id == conversationId && c.Participants.Any(u => u.Id == userId), ct);
    }

    public async Task<CursorPaginationResponse<List<Conversation>>> GetConversationsForUserAsync(string userId, int pageSize, string? cursor, string? query, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        IQueryable<Conversation> baseQuery = dbContext.Conversations
            .Include(c => c.Participants)
            .Include(c => c.Creator)
            .Include(c => c.Messages)
            .Include(m => m.Team)
            .Include(m => m.Messages)
            .Where(c => c.Participants.Any(u => u.Id == userId));

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            baseQuery = baseQuery.Where(c => c.Id.CompareTo(cursor) > 0);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            baseQuery = baseQuery.Where(c =>
                c.Title != null && c.Title.ToLower().Contains(query.ToLower()));
        }
        
        baseQuery = baseQuery.OrderByDescending(c => c.LastMessageAtUtc);

        return await PaginateAsync(baseQuery, pageSize, cursor, query);
    }

    public void Add(Conversation entity, ApplicationDbContext dbContext)
    {
        dbContext.Conversations.Add(entity);
    }

    public void Update(Conversation entity, ApplicationDbContext dbContext)
    {
        dbContext.Conversations.Update(entity);
    }

    public void Remove(Conversation entity, ApplicationDbContext dbContext)
    {
        dbContext.Conversations.Remove(entity);
    }
}