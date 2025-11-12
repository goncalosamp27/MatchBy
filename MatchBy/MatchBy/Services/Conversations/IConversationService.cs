using FluentValidation;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.Services.Conversations;

public interface IConversationService
{
    Task<Result<CursorPaginationResponse<List<ConversationDto>>>> GetConversationsAsync(string creatorUserId,
        int pageSize,
        string? cursor, string? query, CancellationToken ct = default);

    Task<Result<ConversationDto>> GetConversationByIdAsync(string conversationId, string creatorUserId,
        CancellationToken ct = default);

    Task<Result<ConversationDto>> CreateConversationAsync(CreateConversationDto createConversationDto,
        CancellationToken ct = default);

    Task<Result<ConversationDto>> UpdateConversationAsync(UpdateConversationDto updateConversationDto,
        CancellationToken ct = default);

    Task<Result<bool>> DeleteConversationAsync(string conversationId, string userId, CancellationToken ct = default);
    Task<Result<int>> LeaveConversationAsync(string conversationId, string userId, CancellationToken ct = default);
}
