using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;

namespace MatchBy.Services.ChatMessages;

public interface IChatMessageService
{
    Task<Result<List<ChatMessageDto>>> GetChatMessagesAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<Result<ChatMessageDto>> GetChatMessageByIdAsync(string chatMessageId, string userId, CancellationToken ct = default);
    Task<Result<ChatMessageDto>> CreateChatMessageAsync(CreateChatMessageDto createChatMessageDto, CancellationToken ct = default);
    Task<Result<ChatMessageDto>> UpdateChatMessageAsync(UpdateChatMessageDto updateChatMessageDto, CancellationToken ct = default);
    Task<Result<bool>> DeleteChatMessageAsync(string chatMessageId, string userId, CancellationToken ct = default);
}
