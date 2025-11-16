using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.Models;
using MatchBy.Enums;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Matches;

public class MatchesService(ApplicationDbContext applicationDbContext, IMatchEmailSender emailSender) : IMatchesService
{
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchStatus? matchStatus, string? q,
        string? userId, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<Match> query = applicationDbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator);

        List<MatchInvite> invites = await applicationDbContext
            .MatchInvites
            .Where(mi => mi.ReceiverId == userId)
            .ToListAsync(ct);

        // Filter by userId: if provided, get matches where user is creator or participant; else get only public matches
        query = !string.IsNullOrEmpty(userId)
            ? query.Where(m =>
                m.CreatorId == userId || m.Participants.Any(p => p.Id == userId) || m.Privacy == MatchPrivacy.Public ||
                invites.Any(i => i.MatchId == m.Id))
            : query.Where(m => m.Privacy == MatchPrivacy.Public);

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

    public async Task<Result<MatchDto>> GetMatchById(string matchId, string? userId, CancellationToken ct = default)
    {
        Match? match;
        if (!string.IsNullOrEmpty(userId))
        {
            bool hasInvite = await applicationDbContext
                .MatchInvites
                .AnyAsync(i => i.MatchId == matchId && i.ReceiverId == userId, ct);

            match = await applicationDbContext
                .Matches
                .Include(m => m.Participants)
                .Include(m => m.Creator)
                .Where(m => m.Participants.Any(p => p.Id == userId) || m.CreatorId == userId ||
                            m.Privacy == MatchPrivacy.Public || hasInvite)
                .FirstOrDefaultAsync(m => m.Id == matchId, ct);

            return match == null
                ? Result<MatchDto>.Fail($"Match with id {matchId} not found.")
                : Result<MatchDto>.Ok(match.ToDto());
        }

        match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.Privacy == MatchPrivacy.Public)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        return match == null
            ? Result<MatchDto>.Fail($"Match with id {matchId} not found.")
            : Result<MatchDto>.Ok(match.ToDto());
    }

    public async Task<Result<bool>> CreateMatch(CreateMatchDto createMatchDto, CancellationToken ct = default)
    {
        Match match = createMatchDto.ToEntity();
        match.Participants = (List<ApplicationUser>)
            [await applicationDbContext.Users.FirstAsync(u => u.Id == createMatchDto.CreatorId, ct)];
        await applicationDbContext.Matches.AddAsync(match, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateMatch(UpdateMatchDto updateMatchDto, CancellationToken ct = default)
    {
        Match? match = await applicationDbContext
            .Matches
            .Where(m => m.CreatorId == updateMatchDto.UserId)
            .FirstOrDefaultAsync(m => m.Id == updateMatchDto.MatchId, ct);
        if (match is null)
        {
            return Result<bool>.Fail(
                $"Match with id {updateMatchDto.MatchId} not found or user {updateMatchDto.UserId} is not the creator.");
        }

        match.UpdateEntity(updateMatchDto);
        await applicationDbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteMatch(string matchId, string userId, CancellationToken ct = default)
    {
        int result = await applicationDbContext
            .Matches
            .Where(m => m.Id.Equals(matchId))
            .Where(m => m.CreatorId.Equals(userId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.DeletedAtUtc, DateTime.UtcNow), ct);

        return result == 0
            ? Result<bool>.Fail($"Match with id {matchId} not found or user {userId} is not the creator.")
            : Result<bool>.Ok(true);
    }

    public async Task<Result<MatchDto>> JoinMatch(string matchId, string userId, CancellationToken ct = default)
    {
        bool invited = await applicationDbContext
            .MatchInvites
            .Where(mi => mi.ReceiverId == userId)
            .AnyAsync(i => i.MatchId == matchId,ct);

        Match? match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .Where(m => m.Privacy == MatchPrivacy.Public || invited)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found.");
        }

        if (match.Participants.Count >= match.maxPlayers)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} too many participants.");
            // notify user that match is full, with background job?
        }

        if (match.Participants.Any(u => u.Id == userId))
        {
            return Result<MatchDto>.Fail($"User with id {userId} already joined the match {matchId}.");
        }

        ApplicationUser? user = await applicationDbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }

        match.Participants.Add(user);
        await applicationDbContext.SaveChangesAsync(ct);
        return await GetMatchById(matchId, userId, ct);
    }
    
    public async Task<Result<MatchDto>> LeaveMatch(string matchId, string userId, CancellationToken ct = default)
    {
        ApplicationUser? user = await applicationDbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }
    
        Match? match = await applicationDbContext
            .Matches
            .Include(m => m.Participants)
            .Where(m => m.Participants.Any(p => p.Id == userId))
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);
        if (match is null)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found.");
        }
        if (match.CreatorId == userId)
        {
            match.DeletedAtUtc = DateTime.UtcNow;
            match.Status = MatchStatus.Cancelled;
            
            // Send cancellation emails to all participants
            foreach (ApplicationUser participant in match.Participants.Where(p => p.Id != userId))
            {
                if (participant.Email != null)
                {
                    await emailSender.SendMatchCancelledAsync(
                        participant,
                        participant.Email,
                        match,
                        user.DisplayName ?? "Unknown"
                    );
                }
            }
            
            match.Participants.Clear();
        }
        else
        {
            match.Participants.Remove(user);
        }
        await applicationDbContext.SaveChangesAsync(ct);
        return await GetMatchById(matchId, userId, ct);
    }

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesForUser(string userId, string? q,
        int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<Match> query = applicationDbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId == userId);

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

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesExceptUser(string userId, string? q,
        int page = 1, int pageSize = 5, CancellationToken ct = default)
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
