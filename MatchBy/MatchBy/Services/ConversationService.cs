using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services;

public class ConversationService(ApplicationDbContext applicationDbContext) : IConversationService
{
    public async Task<List<Conversation>> GetConversationsAsync(string userId, CancellationToken ct = default)
    {
        List<Conversation> conversations = await applicationDbContext
            .Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .ThenInclude(c => c.Sender)
            .Where(m => m.CreatorId == userId)
            .Where(m => m.Participants.Any(p => p.Id == userId))
            .AsNoTracking()
            .ToListAsync(ct);
        return conversations;
    }

    public async Task<Conversation?> GetConversationByIdAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        Conversation? conversation = await applicationDbContext.Conversations
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Messages)
            .ThenInclude(c => c.Sender)
            .Where(m => m.Id.Equals(conversationId))
            .Where(m => m.CreatorId == userId)
            .Where(m => m.Participants.Any(p => p.Id == userId))
            .FirstOrDefaultAsync(ct);

        return conversation;
    }

    public async Task<bool> CreateConversationAsync(Conversation conversation, string creatorUserId,
        CancellationToken ct = default)
    {
        conversation.Id = $"conversation_{Guid.CreateVersion7()}";
        conversation.CreatorId = creatorUserId;
        conversation.CreatedAtUtc = DateTime.UtcNow;
        conversation.UpdatedAtUtc = null;
        conversation.DeletedAtUtc = null;

        await applicationDbContext.Conversations.AddAsync(conversation, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateConversationAsync(string conversationId, Conversation updated, string userId,
        CancellationToken ct = default)
    {
        // só permite editar se for o criador
        Conversation? convo = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId && c.DeletedAtUtc == null)
            .FirstOrDefaultAsync(ct);

        if (convo is null || convo.CreatorId != userId)
        {
            return false;
        }

        // mapeia apenas campos permitidos. NÃO mudes o CreatorId.
        convo.Type = updated.Type;
        convo.Title = updated.Title;
        convo.Image = updated.Image;
        convo.TeamId = updated.TeamId;
        convo.MatchId = updated.MatchId;
        convo.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteConversationAsync(string conversationId, string userId,
        CancellationToken ct = default)
    {
        // só criador pode apagar
        bool canDelete = await applicationDbContext.Conversations
            .AnyAsync(c => c.Id == conversationId && c.CreatorId == userId && c.DeletedAtUtc == null, ct);

        if (!canDelete)
        {
            return false;
        }

        int affected = await applicationDbContext.Conversations
            .Where(c => c.Id == conversationId && c.DeletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        return affected == 1;
    }
}
