using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Models;
using MatchBy.Enums; 
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Matches;

public class MatchesService(ApplicationDbContext applicationDbContext) : IMatchesService
{
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchStatus? matchStatus, string? q, string? userId, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<Match> query = applicationDbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator);
        
        // Filter by userId: if provided, get matches where user is creator or participant; else get only public matches
        query = !string.IsNullOrEmpty(userId) ? query.Where(m => m.CreatorId == userId || m.Participants.Any(p => p.Id == userId)) : query.Where(m => m.Privacy == MatchPrivacy.Public);

        query = matchStatus switch
        {
            MatchStatus.Pendent => query.Where(m => m.Status == MatchStatus.Pendent),
            MatchStatus.Cancelled => query.Where(m => m.Status == MatchStatus.Cancelled),
            MatchStatus.Completed => query.Where(m => m.Status == MatchStatus.Completed),
            MatchStatus.Confirmed => query.Where(m => m.Status == MatchStatus.Confirmed),
            _ => query
        };

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(m => m.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        
        List<Match> matches = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        var matchesDto = matches.Select(m => m.ToDto()).ToList();
        
        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = matchesDto,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }
    
    public async Task<Result<MatchDto>> GetMatchById(string matchId)
    {
        Match? match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .FirstOrDefaultAsync(m => m.Id == matchId);
        
        return match == null ? Result<MatchDto>.Fail($"Match with id {matchId} not found.") : Result<MatchDto>.Ok(match.ToDto());
    }

    public async Task<Result<bool>> CreateMatch(CreateMatchDto createMatchDto, CancellationToken ct = default)
    {
        Match match = createMatchDto.ToEntity();
        await applicationDbContext.Matches.AddAsync(match, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateMatch(UpdateMatchDto updateMatchDto, CancellationToken ct = default)
    {
        Match? match = await applicationDbContext
            .Matches
            .FirstOrDefaultAsync(m => m.Id == updateMatchDto.MatchId, ct);
        if (match is null)
        {
            return Result<bool>.Fail($"Match with id {updateMatchDto.MatchId} not found.");
        }
        
        match.UpdateEntity(updateMatchDto);
        await applicationDbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteMatch(string matchId)
    {
        await applicationDbContext.Matches.Where(m => m.Id.Equals(matchId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.DeletedAtUtc, DateTime.UtcNow));
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesForUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<Match> query = applicationDbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId == userId );

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(m => m.Description.ToLower().Contains(q.ToLower()));
        }
        
        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        
        List<Match> matches = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        var matchesDto = matches.Select(m => m.ToDto()).ToList();
        
        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = matchesDto,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesExceptUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<Match> query = applicationDbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId != userId && m.Participants.All(p => p.Id != userId));
            
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(m => m.Description.ToLower().Contains(q.ToLower()));
        }
            
        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        
        List<Match> matches = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        var matchesDto = matches.Select(m => m.ToDto()).ToList();
        
        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = matchesDto,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }
}
