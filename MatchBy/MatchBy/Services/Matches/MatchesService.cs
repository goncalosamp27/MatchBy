using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.Notification;
using MatchBy.Models;
using MatchBy.Enums;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using IEmailSender = MatchBy.Services.Email.IEmailSender;

namespace MatchBy.Services.Matches;

public class MatchesService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateMatchDto> createMatchValidator,
    IValidator<UpdateMatchDto> updateMatchValidator, 
    IEmailSender emailSender,
    INotificationService notificationService) : IMatchesService
{
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchStatus? matchStatus, string? q,
        string? userId, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .Include(m => m.Participants)
            .Include(m => m.Creator);

        List<MatchInvite> invites = await dbContext
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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match? match;
        if (!string.IsNullOrEmpty(userId))
        {
            bool hasInvite = await dbContext
                .MatchInvites
                .AnyAsync(i => i.MatchId == matchId && i.ReceiverId == userId, ct);

            match = await dbContext
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

        match = await dbContext
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
        ValidationResult? validationResult = await createMatchValidator.ValidateAsync(createMatchDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<bool>.Fail(validationResult.ToString());
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match match = createMatchDto.ToEntity();
        match.Participants = (List<ApplicationUser>)
            [await dbContext.Users.FirstAsync(u => u.Id == createMatchDto.CreatorId, ct)];
        await dbContext.Matches.AddAsync(match, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateMatch(UpdateMatchDto updateMatchDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateMatchValidator.ValidateAsync(updateMatchDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<bool>.Fail(validationResult.ToString());
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match? match = await dbContext
            .Matches
            .Where(m => m.CreatorId == updateMatchDto.UserId)
            .FirstOrDefaultAsync(m => m.Id == updateMatchDto.MatchId, ct);
        if (match is null)
        {
            return Result<bool>.Fail(
                $"Match with id {updateMatchDto.MatchId} not found or user {updateMatchDto.UserId} is not the creator.");
        }

        match.UpdateEntity(updateMatchDto);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteMatch(string matchId, string userId, CancellationToken ct = default)
    {       
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        int result = await dbContext
            .Matches
            .Where(m => m.Id == matchId)
            .Where(m => m.CreatorId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.DeletedAtUtc, DateTime.UtcNow), ct);

        return result == 0
            ? Result<bool>.Fail($"Match with id {matchId} not found or user {userId} is not the creator.")
            : Result<bool>.Ok(true);
    }

    public async Task<Result<MatchDto>> JoinMatch(string matchId, string userId, CancellationToken ct = default)
    {        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        bool invited = await dbContext
            .MatchInvites
            .Where(mi => mi.ReceiverId == userId)
            .AnyAsync(i => i.MatchId == matchId,ct);

        Match? match = await dbContext
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

        ApplicationUser? user = await dbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }

        match.Participants.Add(user);
        await dbContext.SaveChangesAsync(ct);

        // Notify match creator that someone joined
        if (match.CreatorId == userId)
        {
            return await GetMatchById(matchId, userId, ct);
        }

        string matchName = $"{match.Sport} em {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm}";
        
        string? newParticipant = await dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);
        
        var notification = new CreateNotificationDto
        {
            Type = NotificationType.MatchJoined,
            ReceiverUserId = match.CreatorId,
            SenderUserId = userId,
            RelatedEntityId = match.Id,
            RelatedEntityName = matchName,
            Title = "New participant",
            Message = $"{newParticipant} joined the match {matchName}",
            ActionUrl = $"/matches/{match.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return await GetMatchById(matchId, userId, ct);
    }
    
    public async Task<Result<bool>> LeaveMatch(string matchId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        ApplicationUser? user = await dbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<bool>.Fail($"User with id {userId} not found.");
        }
        
        Match? match = await dbContext
            .Matches
            .Include(m => m.Participants)
            .Where(m => m.Participants.Any(p => p.Id == userId))
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return Result<bool>.Fail($"Match with id {matchId} not found.");
        }

        if (match.CreatorId == userId)
        {
            match.Status = MatchStatus.Cancelled;
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

        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesForUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default) {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId == userId && m.Participants.Count < m.maxPlayers && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent));

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
            new PaginationResponse<List<MatchDto>> {
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
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
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

    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesUserAttending(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default) {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId != userId && m.Participants.Any(p => p.Id == userId) && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent));

        if (!string.IsNullOrEmpty(q)) {
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

    public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
