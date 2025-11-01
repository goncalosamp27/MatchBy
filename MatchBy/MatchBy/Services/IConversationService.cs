using MatchBy.Models;

namespace MatchBy.Services;

public interface IConversationService
{
    Task<List<Conversation>> GetConversationsAsync(string userId, CancellationToken ct = default);
    Task<Conversation?> GetConversationByIdAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<bool> CreateConversationAsync(Conversation conversation, string creatorUserId, CancellationToken ct = default);

    Task<bool> UpdateConversationAsync(string conversationId, Conversation updated, string userId,
        CancellationToken ct = default);

    Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct = default);
}
