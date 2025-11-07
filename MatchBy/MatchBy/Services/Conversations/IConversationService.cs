using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.Services.Conversations;

public interface IConversationService
{
    Task<List<ConversationDto>> GetConversationsAsync(string creatorUserId, CancellationToken ct = default);
    Task<ConversationDto?> GetConversationByIdAsync(string conversationId, string creatorUserId, CancellationToken ct = default);
    Task<ConversationDto?> CreateConversationAsync(CreateConversationDto createConversationDto, CancellationToken ct = default);

    Task<ConversationDto?> UpdateConversationAsync(UpdateConversationDto updateConversationDto,
        CancellationToken ct = default);

    Task<bool> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<bool> LeaveConversationAsync(string conversationId, string userId, CancellationToken ct = default);
}
