using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.Services;

public interface IConversationService
{
    Task<List<Conversation>> GetConversationsAsync(string creatorUserId, CancellationToken ct = default);
    Task<Conversation?> GetConversationByIdAsync(string conversationId, string creatorUserId, CancellationToken ct = default);
    Task<Conversation?> CreateConversationAsync(string creatorUserId, ConversationType conversationType, List<string> participantIds,
        string? title, string? teamId, string? matchId, CancellationToken ct = default);

    Task<Conversation?> UpdateConversationAsync(string conversationId, string creatorUserId, List<string> participantIds,
        CancellationToken ct = default);

    Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<bool> LeaveConversationAsync(string conversationId, string userId, CancellationToken ct = default);

    Task<Conversation?> UpdateConversationImageAsync(string conversationId, string userId, IBrowserFile file, CancellationToken ct = default);
}
