using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.Friend;

public interface IFriendRepository
{
    Task<PaginationResponse<List<Models.Friend>>> GetUserFriends(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10,
        CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Friend>>> GetFriendRequestsSent(string userId, ApplicationDbContext dbContext, int page = 1, int pageSize = 10,
        CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Friend>>> GetFriendRequestsReceived(string userId, ApplicationDbContext dbContext, int page = 1,
        int pageSize = 10, CancellationToken ct = default);
    Task<Models.Friend?> GetByIdAsync(string friendshipId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<Models.Friend?> ExistsAsync(string user1, string user2, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.Friend entity, ApplicationDbContext dbContext);
    void Update(Models.Friend entity, ApplicationDbContext dbContext);
    void Remove(Models.Friend entity, ApplicationDbContext dbContext);
}