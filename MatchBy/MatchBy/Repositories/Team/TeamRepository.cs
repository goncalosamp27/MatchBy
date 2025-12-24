using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.Team;

public class TeamRepository : ITeamRepository
{
    /// <summary>
    /// Creates a base queryable for teams with all necessary includes (owner, members, conversation, messages).
    /// </summary>
    /// <param name="dbContext">The database context to query from.</param>
    /// <returns>A queryable collection of teams with all related entities included.</returns>
    private static IQueryable<Models.Team> Query(ApplicationDbContext dbContext)
    {
        return dbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages);
    }

    public async Task<Models.Team?> GetByIdAsync(string teamId, string userId, bool hasInvite,
        ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        Models.Team? team = await Query(dbContext)
            .Where(m => m.Id == teamId)
            .Where(m => m.Privacy == TeamPrivacy.Public || hasInvite || m.Members.Any(u => u.Id == userId))
            .FirstOrDefaultAsync(ct);

        return team;
    }

    public async Task<Models.Team?> GetTeamUserOwnsByIdAsync(string teamId, string ownerId,
        ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        Models.Team? team = await Query(dbContext)
            .Where(m => m.Id == teamId)
            .Where(m => m.OwnerId == ownerId)
            .FirstOrDefaultAsync(ct);

        return team;
    }

    public async Task<Models.Team?> GetTeamUserParticipatesByIdAsync(string teamId, string userId,
        ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        Models.Team? team = await Query(dbContext)
            .Where(m => m.Id == teamId)
            .Where(m => m.Members.Any(u => u.Id == userId))
            .FirstOrDefaultAsync(ct);

        return team;
    }

    public async Task<PaginationResponse<List<Models.Team>>> GetTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, List<string> userPendingInvites, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        return await PaginateTeamsWithFiltersAsync(
            Query(dbContext),
            userPendingInvites,
            teamQueryParametersDto,
            ct);
    }

    public async Task<PaginationResponse<List<Models.Team>>> GetAvailableTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, List<string> userPendingInvites,
        ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        IQueryable<Models.Team> query = Query(dbContext)
            .Where(t => t.Members.All(u => u.Id != teamQueryParametersDto.UserId) &&
                        t.OwnerId != teamQueryParametersDto.UserId)
            .AsNoTracking();

        return await PaginateTeamsWithFiltersAsync(query, userPendingInvites, teamQueryParametersDto, ct);
    }

    public async Task<PaginationResponse<List<Models.Team>>> GetTeamsUserOwnAsync(string userId, int page, int pageSize,
        string q, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        IQueryable<Models.Team> query = Query(dbContext)
            .Where(m => m.OwnerId == userId);

        return await PaginateTeamsAsync(query, page, pageSize, q, ct);
    }

    public async Task<PaginationResponse<List<Models.Team>>> GetTeamsUserParticipateAsync(string userId, int page,
        int pageSize, string q, ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        IQueryable<Models.Team> query = Query(dbContext)
            .Where(m => m.Members.Any(u => u.Id == userId))
            .AsNoTracking();

        return await PaginateTeamsAsync(query, page, pageSize, q, ct);
    }

    public void Add(Models.Team entity, ApplicationDbContext dbContext)
    {
        dbContext.Teams.Add(entity);
    }

    public void Update(Models.Team entity, ApplicationDbContext dbContext)
    {
        dbContext.Teams.Update(entity);
    }

    public void Remove(Models.Team entity, ApplicationDbContext dbContext)
    {
        dbContext.Teams.Remove(entity);
    }

    /// <summary>
    /// Paginates and filters a queryable collection of teams, refreshing images in parallel for better performance.
    /// </summary>
    /// <param name="query">The queryable collection of teams to paginate.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of teams per page.</param>
    /// <param name="q">Optional search query to filter teams by name or description (case-insensitive).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with a list of team DTOs, ordered by creation date (newest first).
    /// </returns>
    private static async Task<PaginationResponse<List<Models.Team>>> PaginateTeamsAsync(
        IQueryable<Models.Team> query, int page, int pageSize, string q, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.Name.ToLower().Contains(q.ToLower()) || c.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Models.Team> teams = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PaginationResponse<List<Models.Team>>
        {
            Data = teams,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Paginates teams with advanced filtering including sorting, ordering, and privacy filtering.
    /// </summary>
    /// <param name="query">The queryable collection of teams to filter and paginate.</param>
    /// <param name="invitedTeamIds">List of team IDs for which the user has pending invites.</param>
    /// <param name="teamQueryParametersDto">DTO containing filter parameters (sort, order, privacy, query, pagination).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with filtered and sorted team DTOs.
    /// </returns>
    /// <remarks>
    /// This method applies sorting (by name, description, created date, or member count),
    /// ordering (ascending/descending), and privacy filtering (public, private, or all).
    /// Private teams are only visible if the user is a member or has a pending invite.
    /// </remarks>
    private async Task<PaginationResponse<List<Models.Team>>> PaginateTeamsWithFiltersAsync(
        IQueryable<Models.Team> query, List<string> invitedTeamIds, TeamQueryParametersDto teamQueryParametersDto,
        CancellationToken ct = default)
    {
#pragma warning disable IDE0066
        switch (teamQueryParametersDto.SortBy)
        {
            case SortBy.Description:
                query = teamQueryParametersDto.OrderBy == OrderBy.Ascending
                    ? query.OrderBy(t => t.Description)
                    : query.OrderByDescending(t => t.Description);
                break;
            case SortBy.CreatedAt:
                query = teamQueryParametersDto.OrderBy == OrderBy.Ascending
                    ? query.OrderBy(t => t.CreatedAtUtc)
                    : query.OrderByDescending(t => t.CreatedAtUtc);
                break;
            case SortBy.MembersCount:
                query = teamQueryParametersDto.OrderBy == OrderBy.Ascending
                    ? query.OrderBy(t => t.Members.Count)
                    : query.OrderByDescending(t => t.Members.Count);
                break;
            default:
                query = teamQueryParametersDto.OrderBy == OrderBy.Ascending
                    ? query.OrderBy(t => t.Name)
                    : query.OrderByDescending(t => t.Name);
                break;
        }

        switch (teamQueryParametersDto.Privacy)
        {
            case Privacy.Public:
                query = query.Where(t => t.Privacy == TeamPrivacy.Public);
                break;
            case Privacy.Private:
                query = query.Where(t =>
                    t.Privacy == TeamPrivacy.Private && (t.Members.Any(u => u.Id == teamQueryParametersDto.UserId) ||
                                                         invitedTeamIds.Contains(t.Id)));
                break;
            case Privacy.All:
                query = query.Where(t => t.Privacy == TeamPrivacy.Public || t.Privacy == TeamPrivacy.Private &&
                    (t.Members.Any(u => u.Id == teamQueryParametersDto.UserId) || invitedTeamIds.Contains(t.Id)));
                break;
            default:
                query = query.Where(t => t.Privacy == TeamPrivacy.Public || t.Privacy == TeamPrivacy.Private &&
                    (t.Members.Any(u => u.Id == teamQueryParametersDto.UserId) || invitedTeamIds.Contains(t.Id)));
                break;
        }
#pragma warning restore IDE0066

        return await PaginateTeamsAsync(
            query,
            teamQueryParametersDto.Page,
            teamQueryParametersDto.PageSize,
            teamQueryParametersDto.Query,
            ct);
    }
}