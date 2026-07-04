using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.PlayerRating;

public interface IPlayerRatingRepository
{
    Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsForMatch(string matchId, ApplicationDbContext dbContext, int page = 1,
        int pageSize = 10, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsGivenByUser(string userId,
        ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsReceivedByUser(string userId,
        ApplicationDbContext dbContext, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Models.PlayerRating?> GetByIdAsync(string ratingId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<Models.PlayerRating?> GetByIdAsync(string sentById, string receivedById, string matchId, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.PlayerRating entity, ApplicationDbContext dbContext);
    void Update(Models.PlayerRating entity, ApplicationDbContext dbContext);
    void Remove(Models.PlayerRating entity, ApplicationDbContext dbContext);
}