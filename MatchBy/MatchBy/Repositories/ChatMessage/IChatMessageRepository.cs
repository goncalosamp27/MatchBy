using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.ChatMessage;

public interface IChatMessageRepository
{
    Task<CursorPaginationResponse<List<Models.ChatMessage>>> GetChatMessagesAsync(
        string conversationId,
        string userId,
        ApplicationDbContext dbContext,
        int pageSize,
        string? cursor,
        CancellationToken ct = default);
    Task<Models.ChatMessage?> GetByIdAsync(string chatMessageId, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.ChatMessage entity, ApplicationDbContext dbContext);
    void Update(Models.ChatMessage entity, ApplicationDbContext dbContext);
    void Remove(Models.ChatMessage entity, ApplicationDbContext dbContext);
}