using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.PlayerRating;

public class PlayerRatingRepository: IPlayerRatingRepository
{
    private static IQueryable<Models.PlayerRating> Query(ApplicationDbContext dbContext)
    {
        return dbContext.PlayerRatings
            .Include(r => r.SentBy)
            .Include(r => r.ReceivedBy)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Creator)
            .Include(r => r.Match)
            .ThenInclude(m => m!.Participants);
    }

    public async Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsForMatch(string matchId,ApplicationDbContext dbContext,  int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.PlayerRating> query = Query(dbContext)
            .Where(r => r.MatchId == matchId);

        int total = await query.CountAsync(ct);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.PlayerRating>>
        {
            Data = ratings,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
    
    public async Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsGivenByUser(string userId, ApplicationDbContext dbContext,  int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.PlayerRating> query = Query(dbContext)
            .Where(r => r.SentById == userId);

        int total = await query.CountAsync(ct);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.PlayerRating>>
        {
            Data = ratings,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
    
    public async Task<PaginationResponse<List<Models.PlayerRating>>> GetRatingsReceivedByUser(string userId, ApplicationDbContext dbContext,  int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<Models.PlayerRating> query = Query(dbContext)
            .Where(r => r.ReceivedById == userId);

        int total = await query.CountAsync(ct);

        List<Models.PlayerRating> ratings = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.PlayerRating>>
        {
            Data = ratings,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<Models.PlayerRating?> GetByIdAsync(string ratingId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return Query(dbContext).FirstOrDefaultAsync(r => r.Id == ratingId, ct);
    }

    public Task<Models.PlayerRating?> GetByIdAsync(string sentById, string receivedById, string matchId, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        return Query(dbContext).FirstOrDefaultAsync(r => r.SentById == sentById && r.ReceivedById == receivedById && r.MatchId == matchId, ct);
    }

    public void Add(Models.PlayerRating entity, ApplicationDbContext dbContext)
    {
        dbContext.PlayerRatings.Add(entity);
    }

    public void Update(Models.PlayerRating entity, ApplicationDbContext dbContext)
    {
        dbContext.PlayerRatings.Update(entity);
    }

    public void Remove(Models.PlayerRating entity, ApplicationDbContext dbContext)
    {
        dbContext.PlayerRatings.Remove(entity);
    }
}