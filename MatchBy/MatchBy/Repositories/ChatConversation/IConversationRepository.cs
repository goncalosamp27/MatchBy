using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.ChatConversation;

public interface IConversationRepository
{
    Task<bool> PrivateConversationExistsAsync(IReadOnlyCollection<string> participantIds, ApplicationDbContext dbContext, CancellationToken ct);
    Task<Conversation?> GetByIdAsync(string conversationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<bool> IsParticipantAsync(string conversationId, string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<CursorPaginationResponse<List<Conversation>>> GetConversationsForUserAsync(string userId, int pageSize, string? cursor, string? query, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Conversation entity, ApplicationDbContext dbContext);
    void Update(Conversation entity, ApplicationDbContext dbContext);
    void Remove(Conversation entity, ApplicationDbContext dbContext);
}