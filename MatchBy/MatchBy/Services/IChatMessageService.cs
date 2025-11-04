using MatchBy.Models;

namespace MatchBy.Services;

public interface IChatMessageService
{
    Task<List<ChatMessage>> GetChatMessagesAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<ChatMessage?> GetChatMessageByIdAsync(string chatMessageId, string userId, CancellationToken ct = default);
    Task<ChatMessage?> CreateChatMessageAsync(string content, string creatorUserId, string conversationId, string? replyToMessageId = null, CancellationToken ct = default);
    Task<ChatMessage?> UpdateChatMessageAsync(string chatMessageId, string content, string creatorUserId, CancellationToken ct = default);
    Task<bool> DeleteChatMessageAsync(string chatMessageId, string userId, CancellationToken ct = default);
}
