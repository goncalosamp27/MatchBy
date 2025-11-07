using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;

namespace MatchBy.Services.ChatMessages;

public interface IChatMessageService
{
    Task<List<ChatMessageDto>> GetChatMessagesAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<ChatMessageDto?> GetChatMessageByIdAsync(string chatMessageId, string userId, CancellationToken ct = default);
    Task<ChatMessageDto?> CreateChatMessageAsync(CreateChatMessageDto createChatMessageDto, CancellationToken ct = default);
    Task<ChatMessageDto?> UpdateChatMessageAsync(UpdateChatMessageDto updateChatMessageDto, CancellationToken ct = default);
    Task<bool> DeleteChatMessageAsync(string chatMessageId, string userId, CancellationToken ct = default);
}
