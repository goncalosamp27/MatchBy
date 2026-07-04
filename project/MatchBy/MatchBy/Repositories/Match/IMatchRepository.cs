using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Models;

namespace MatchBy.Repositories.Match;

public interface IMatchRepository
{
    Task<List<string>> GetAllMatchCountries(ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<List<string>> GetAllCitiesByCountry(string country,ApplicationDbContext dbContext, CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Match>>> GetMatchesUserAttending(string userId,ApplicationDbContext dbContext, string? q, int page = 1,
        int pageSize = 5, CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Match>>> GetMatchesExceptUser(string userId,ApplicationDbContext dbContext, string? q,
        int page = 1, int pageSize = 5, CancellationToken ct = default);

    Task<PaginationResponse<List<Models.Match>>> GetMatchesForUser(string userId, ApplicationDbContext dbContext, string? q, int page = 1,
        int pageSize = 5, CancellationToken ct = default);
    Task<PaginationResponse<List<Models.Match>>> GetMatches(MatchQueryParametersDto matchQueryParametersDto, List<string> invitedMatchIds, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<bool> ExistsAsync(string matchId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<Models.Match?> GetByIdAsync(string matchId, string? userId, bool hasPendingInvite, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(Models.Match entity, ApplicationDbContext dbContext);
    void Update(Models.Match entity, ApplicationDbContext dbContext);
    void Remove(Models.Match entity, ApplicationDbContext dbContext);
}