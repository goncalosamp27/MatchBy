using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Utils;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.Match;

public class MatchRepository : IMatchRepository
{
    private static IQueryable<Models.Match> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .Matches
            .Include(m => m.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(m => m.Creator)
            .Include(m => m.Participants);
    }

    public async Task<List<string>> GetAllMatchCountries(ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await Query(dbContext)
            .Select(m => m.Location.Country)
            .Distinct()
            .Where(c => !string.IsNullOrEmpty(c))
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetAllCitiesByCountry(string country, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        return await Query(dbContext)
            .Where(m => m.Location.Country.ToLower() == country.ToLower())
            .Select(m => m.Location.City)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    public async Task<PaginationResponse<List<Models.Match>>> GetMatchesUserAttending(string userId,
        ApplicationDbContext dbContext, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        return await PaginateMatchesAsync(Query(dbContext)
                .Where(m => m.CreatorId != userId && m.Participants.Any(p => p.Id == userId) &&
                            (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent)), q, page, pageSize,
            ct: ct);
    }

    public async Task<PaginationResponse<List<Models.Match>>> GetMatchesExceptUser(string userId,
        ApplicationDbContext dbContext, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        return await PaginateMatchesAsync(Query(dbContext)
                .Where(m => m.CreatorId != userId && m.Participants.All(p => p.Id != userId)), q, page, pageSize,
            ct: ct);
    }

    public async Task<PaginationResponse<List<Models.Match>>> GetMatchesForUser(string userId,
        ApplicationDbContext dbContext, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        return await PaginateMatchesAsync(Query(dbContext)
                .Where(m => m.CreatorId == userId && m.Participants.Count < m.MaxPlayers &&
                            (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent)), q, page, pageSize,
            ct: ct);
    }

    public async Task<PaginationResponse<List<Models.Match>>> GetMatches(
        MatchQueryParametersDto matchQueryParametersDto, List<string> invitedMatchIds, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        IQueryable<Models.Match> query = Query(dbContext);

        query = !string.IsNullOrEmpty(matchQueryParametersDto.UserId)
            ? query.Where(m =>
                m.CreatorId == matchQueryParametersDto.UserId ||
                m.Participants.Any(p => p.Id == matchQueryParametersDto.UserId) ||
                m.Privacy == MatchPrivacy.Public || invitedMatchIds.Contains(m.Id))
            : query.Where(m => m.Privacy == MatchPrivacy.Public);

        if (matchQueryParametersDto.SportsList.Any())
        {
            query = query.Where(m => matchQueryParametersDto.SportsList.Contains(m.Sport));
        }

        if (!string.IsNullOrEmpty(matchQueryParametersDto.Country))
        {
            query = query.Where(m => m.Location.Country.ToLower() == matchQueryParametersDto.Country.ToLower());
        }

        if (!string.IsNullOrEmpty(matchQueryParametersDto.City))
        {
            query = query.Where(m => m.Location.City.ToLower() == matchQueryParametersDto.City.ToLower());
        }

        if (matchQueryParametersDto.FromDateUtc.HasValue)
        {
            query = query.Where(m =>
                DateOnly.FromDateTime(m.MatchDateTimeUtc) >= matchQueryParametersDto.FromDateUtc.Value);
        }

        if (matchQueryParametersDto.ToDateUtc.HasValue)
        {
            query = query.Where(m =>
                DateOnly.FromDateTime(m.MatchDateTimeUtc) <= matchQueryParametersDto.ToDateUtc.Value);
        }

        if (matchQueryParametersDto.FromTimeUtc.HasValue)
        {
            query = query.Where(m =>
                m.MatchDateTimeUtc.TimeOfDay >= TimeSpan.FromHours(matchQueryParametersDto.FromTimeUtc.Value));
        }

        if (matchQueryParametersDto.ToTimeUtc.HasValue)
        {
            query = query.Where(m =>
                m.MatchDateTimeUtc.TimeOfDay <= TimeSpan.FromHours(matchQueryParametersDto.ToTimeUtc.Value));
        }

        if (matchQueryParametersDto.MinimumPlayersAverage != MinimumPlayersAverage.All)
        {
            int minAverage = (int)matchQueryParametersDto.MinimumPlayersAverage;
            query = query.Where(m => m.Participants.Average(p => p.Rating) >= minAverage);
        }

        if (matchQueryParametersDto is { MaxDistanceInKm: not null, UserLatitude: not null, UserLongitude: not null })
        {
            double userLat = matchQueryParametersDto.UserLatitude.Value;
            double userLon = matchQueryParametersDto.UserLongitude.Value;
            int maxDistance = matchQueryParametersDto.MaxDistanceInKm.Value;

            const double r = 6371;
            query = query.Where(m =>
                Math.Acos(
                    Math.Sin(m.Location.Latitude * Math.PI / 180) *
                    Math.Sin(userLat * Math.PI / 180) +
                    Math.Cos(m.Location.Latitude * Math.PI / 180) *
                    Math.Cos(userLat * Math.PI / 180) *
                    Math.Cos((m.Location.Longitude - userLon) * Math.PI / 180)
                ) * r <= maxDistance
            );
        }

        query = matchQueryParametersDto.MatchStatus switch
        {
            Status.Pendent => query.Where(m => m.Status == MatchStatus.Pendent),
            Status.Cancelled => query.Where(m => m.Status == MatchStatus.Cancelled),
            Status.Completed => query.Where(m => m.Status == MatchStatus.Completed),
            Status.Confirmed => query.Where(m => m.Status == MatchStatus.Confirmed),
            _ => query
        };

        return await PaginateMatchesAsync(
            query,
            matchQueryParametersDto.Q,
            matchQueryParametersDto.Page,
            matchQueryParametersDto.PageSize,
            matchQueryParametersDto.UserLatitude ?? 0,
            matchQueryParametersDto.UserLongitude ?? 0,
            matchQueryParametersDto.SortBy,
            matchQueryParametersDto.OrderBy,
            ct);
    }

    public async Task<bool> ExistsAsync(string matchId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return await dbContext.Matches.AnyAsync(m => m.Id == matchId, ct);
    }

    public Task<Models.Match?> GetByIdAsync(string matchId, string? userId, bool hasPendingInvite,
        ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        if (userId == null)
        {
            return Query(dbContext)
                .Where(m => m.Privacy == MatchPrivacy.Public)
                .FirstOrDefaultAsync(m => m.Id == matchId, ct);
        }

        return Query(dbContext)
            .Where(m => m.Participants.Any(p => p.Id == userId) || m.CreatorId == userId ||
                        m.Privacy == MatchPrivacy.Public || hasPendingInvite)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);
    }

    public void Add(Models.Match entity, ApplicationDbContext dbContext)
    {
        dbContext.Matches.Add(entity);
    }

    public void Update(Models.Match entity, ApplicationDbContext dbContext)
    {
        dbContext.Matches.Update(entity);
    }

    public void Remove(Models.Match entity, ApplicationDbContext dbContext)
    {
        dbContext.Matches.Remove(entity);
    }

    /// <summary>
    /// Paginates and filters a queryable collection of matches.
    /// </summary>
    /// <param name="query">The queryable collection of matches to paginate.</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="orderBy">The order by which to sort the matches (default: Ascending).</param>
    /// <param name="page">The page number to retrieve (1-based, default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <param name="userLatitude">The latitude of the user for distance calculations (default: 0).</param>
    /// <param name="userLongitude">The longitude of the user for distance calculations (default: 0).</param>
    /// <param name="sortBy">The criteria by which to sort the matches (default: DateCreated).</param>
    /// <returns>
    /// A result containing a paginated response with a list of match DTOs, ordered by creation date (newest first).
    /// </returns>
    private static async Task<PaginationResponse<List<Models.Match>>> PaginateMatchesAsync(
        IQueryable<Models.Match> query, string? q, int page = 1, int pageSize = 5, double userLatitude = 0,
        double userLongitude = 0,
        SortBy sortBy = SortBy.MatchDateTime, OrderBy orderBy = OrderBy.Ascending, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(m => m.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Models.Match> matches;
#pragma warning disable IDE0066
        switch (sortBy)
#pragma warning restore IDE0066
        {
            case SortBy.MatchDateTime when orderBy == OrderBy.Descending:
                matches = await query
                    .OrderByDescending(m => m.MatchDateTimeUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);
                break;
            case SortBy.PlayersAverage when orderBy == OrderBy.Ascending:
                matches = await query
                    .OrderBy(m => m.Participants.Average(p => p.Rating))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);
                break;
            case SortBy.PlayersAverage when orderBy == OrderBy.Descending:
                matches = await query
                    .OrderByDescending(m => m.Participants.Average(p => p.Rating))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);
                break;
            case SortBy.Distance when orderBy == OrderBy.Ascending:
                matches = query
                    .AsEnumerable()
                    .OrderBy(m => GeoUtils.HaversineDistance(
                        m.Location.Latitude,
                        m.Location.Longitude,
                        userLatitude,
                        userLongitude))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                break;
            case SortBy.Distance when orderBy == OrderBy.Descending:
                matches = query
                    .AsEnumerable()
                    .OrderByDescending(m => GeoUtils.HaversineDistance(
                        m.Location.Latitude,
                        m.Location.Longitude,
                        userLatitude,
                        userLongitude))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                break;
#pragma warning disable S3458
            case SortBy.MatchDateTime when orderBy == OrderBy.Ascending:
#pragma warning restore S3458
            default:
                matches = await query
                    .OrderBy(m => m.MatchDateTimeUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);
                break;
        }

        return new PaginationResponse<List<Models.Match>>
        {
            Data = matches,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }
}