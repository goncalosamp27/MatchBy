using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Models;
using MatchBy.Enums;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using IEmailSender = MatchBy.Services.Email.IEmailSender;

namespace MatchBy.Services.Matches;

public class MatchesService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateMatchDto> createMatchValidator,
    IValidator<UpdateMatchDto> updateMatchValidator, 
    IMatchesInvitesService matchInvitesService,
    IEmailSender emailSender,
    INotificationService notificationService) : IMatchesService
{
    /// <summary>
    /// Paginates and filters a queryable collection of matches.
    /// </summary>
    /// <param name="query">The queryable collection of matches to paginate.</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (1-based, default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with a list of match DTOs, ordered by creation date (newest first).
    /// </returns>
    private static async Task<Result<PaginationResponse<List<MatchDto>>>> PaginateMatchesAsync(IQueryable<Match> query, string? q,
        int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
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
    /// <summary>
    /// Retrieves a paginated list of matches with optional filtering by status, search query, and user access.
    /// </summary>
    /// <param name="matchStatus">Optional match status to filter by (Pendent, Cancelled, Completed, Confirmed).</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="userId">Optional user ID. If provided, includes matches where user is creator, participant, has invite, or public matches.</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with filtered match DTOs, or an error if failed to retrieve user invites.
    /// </returns>
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchStatus? matchStatus, string? q,
        string? userId, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .Include(m => m.Participants)
            .Include(m => m.Creator);

        if (!string.IsNullOrEmpty(userId))
        {
            Result<PaginationResponse<List<MatchInviteDto>>> receivedUserInvitesResult = await matchInvitesService.GetReceivedInvites(userId, int.MaxValue, 1, ct);
            if(!receivedUserInvitesResult.Success)
            {
                return Result<PaginationResponse<List<MatchDto>>>.Fail(receivedUserInvitesResult.ErrorMessages[0]);
            }
            
            // Filter by userId: if provided, get matches where user is creator or participant; else get only public matches
            query = query.Where(m =>
                m.CreatorId == userId || m.Participants.Any(p => p.Id == userId) || m.Privacy == MatchPrivacy.Public ||
                receivedUserInvitesResult.Data!.Data.Any(i => i.MatchId == m.Id));
        }
        else
        {
            query = query.Where(m => m.Privacy == MatchPrivacy.Public);   
        }

        query = matchStatus switch
        {
            MatchStatus.Pendent => query.Where(m => m.Status == MatchStatus.Pendent),
            MatchStatus.Cancelled => query.Where(m => m.Status == MatchStatus.Cancelled),
            MatchStatus.Completed => query.Where(m => m.Status == MatchStatus.Completed),
            MatchStatus.Confirmed => query.Where(m => m.Status == MatchStatus.Confirmed),
            _ => query
        };

        return await PaginateMatchesAsync(query, q, page, pageSize, ct);
    }
    /// <summary>
    /// Retrieves a specific match by its unique identifier with access control based on privacy settings and invites.
    /// </summary>
    /// <param name="matchId">The unique identifier of the match to retrieve.</param>
    /// <param name="userId">Optional user ID. If provided, checks for user access via participation, creation, invite, or public access.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the match DTO if found and accessible, or an error message if:
    /// - The match is not found
    /// - Access is denied (private match and user doesn't have access)
    /// - Failed to retrieve match invite
    /// </returns>
    public async Task<Result<MatchDto>> GetMatchById(string matchId, string? userId, CancellationToken ct = default)
    {        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match? match;
        if (!string.IsNullOrEmpty(userId))
        {
            Result<MatchInviteDto> matchInvite = await matchInvitesService.GetMatchInvite(matchId, userId, ct);
            if(!matchInvite.Success)
            {
                return Result<MatchDto>.Fail(matchInvite.ErrorMessages[0]);
            }

            match = await dbContext
                .Matches
                .Include(m => m.Participants)
                .Include(m => m.Creator)
                .Where(m => m.Participants.Any(p => p.Id == userId) || m.CreatorId == userId ||
                            m.Privacy == MatchPrivacy.Public || matchInvite.Success)
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
    /// <summary>
    /// Creates a new match with the specified details.
    /// </summary>
    /// <param name="createMatchDto">DTO containing match creation details (sport, date, location, description, privacy, creator, etc.).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the match was successfully created, or an error message if validation fails.
    /// </returns>
    /// <remarks>
    /// The creator is automatically added as the first participant in the match.
    /// </remarks>
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
    /// <summary>
    /// Updates an existing match's details. Only the match creator can update the match.
    /// </summary>
    /// <param name="updateMatchDto">DTO containing the match update details (id, description, date, location, privacy, userId).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the match was successfully updated, or an error message if:
    /// - Validation fails
    /// - Match not found or user is not the creator
    /// </returns>
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
    /// <summary>
    /// Soft deletes a match. Only the match creator can delete the match.
    /// </summary>
    /// <param name="matchId">The unique identifier of the match to delete.</param>
    /// <param name="userId">The unique identifier of the user attempting to delete the match.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the match was successfully soft deleted, or an error message if:
    /// - Match not found or user is not the creator
    /// </returns>
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
    /// <summary>
    /// Adds a user to a match. For private matches, the user must have a pending invite that is accepted first.
    /// </summary>
    /// <param name="matchId">The unique identifier of the match to join.</param>
    /// <param name="userId">The unique identifier of the user joining the match.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the match DTO if the user successfully joined, or an error message if:
    /// - Failed to retrieve match invite
    /// - Match not found
    /// - Match is already full
    /// - User is already a participant
    /// - User not found
    /// - Failed to accept match invite (for private matches)
    /// </returns>
    /// <remarks>
    /// For public matches, the user is added directly. For private matches or users with pending invites,
    /// the invite must be accepted first. A notification is sent to the match creator when a new participant joins.
    /// </remarks>
    public async Task<Result<MatchDto>> JoinMatch(string matchId, string userId, CancellationToken ct = default)
    {        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Result<MatchInviteDto> matchInvite = await matchInvitesService.GetMatchInvite(matchId, userId, ct);
        if(!matchInvite.Success)
        {
            return Result<MatchDto>.Fail(matchInvite.ErrorMessages[0]);
        }

        Match? match = await dbContext
            .Matches
            .Include(m => m.Participants)
            .Where(m => m.Privacy == MatchPrivacy.Public || matchInvite.Success)
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
        
        if(matchInvite.Success)
        {
            Result<MatchInviteDto> aceptInviteResult = await matchInvitesService.AcceptInvite(matchInvite.Data!.Id, userId, ct);
            if(!aceptInviteResult.Success)
            {
                return Result<MatchDto>.Fail(aceptInviteResult.ErrorMessages[0]);
            }
        }

        match.Participants.Add(user);
        await dbContext.SaveChangesAsync(ct);

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.MatchJoined,
            ReceiverUserId = match.CreatorId,
            SenderUserId = userId,
            RelatedEntityId = match.Id,
            RelatedEntityName = $"{match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm}",
            Title = "New participant",
            Message = $"{user.DisplayName} joined the match {match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm}",
            ActionUrl = $"/matches/{match.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return await GetMatchById(matchId, userId, ct);
    }
    /// <summary>
    /// Removes a user from a match. If the user is the creator, the match is cancelled and all participants are notified via email.
    /// </summary>
    /// <param name="matchId">The unique identifier of the match to leave.</param>
    /// <param name="userId">The unique identifier of the user leaving the match.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the user successfully left the match, or an error message if:
    /// - User not found
    /// - Match not found or user is not a participant
    /// </returns>
    /// <remarks>
    /// If the creator leaves, the match status is set to Cancelled, the match is soft deleted,
    /// and cancellation emails are sent to all other participants.
    /// </remarks>
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
    /// <summary>
    /// Retrieves a paginated list of matches created by a specific user that are not full and are in Pendent or Confirmed status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to get matches for.</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with match DTOs created by the user.
    /// </returns>
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesForUser(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default) {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId == userId && m.Participants.Count < m.maxPlayers && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent));

        return await PaginateMatchesAsync(query, q, page, pageSize, ct);
    }
    /// <summary>
    /// Retrieves a paginated list of matches that the user is not involved in (not creator and not participant).
    /// </summary>
    /// <param name="userId">The unique identifier of the user to exclude matches for.</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with match DTOs that the user can potentially join.
    /// </returns>
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
        
        return await PaginateMatchesAsync(query, q, page, pageSize, ct);
    }
    /// <summary>
    /// Retrieves a paginated list of matches that a user is attending (as participant, not creator) that are in Pendent or Confirmed status.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to get attending matches for.</param>
    /// <param name="q">Optional search query to filter matches by description (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of matches per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with match DTOs the user is attending.
    /// </returns>
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatchesUserAttending(string userId, string? q, int page = 1, int pageSize = 5, CancellationToken ct = default) {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m => m.CreatorId != userId && m.Participants.Any(p => p.Id == userId) && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent));

        return await PaginateMatchesAsync(query, q, page, pageSize, ct);
    }
    /// <summary>
    /// Calculates the distance between two geographic coordinates using the Haversine formula.
    /// </summary>
    /// <param name="lat1">Latitude of the first point in degrees.</param>
    /// <param name="lon1">Longitude of the first point in degrees.</param>
    /// <param name="lat2">Latitude of the second point in degrees.</param>
    /// <param name="lon2">Longitude of the second point in degrees.</param>
    /// <returns>
    /// The distance between the two points in kilometers.
    /// </returns>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
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
    
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetRecommendedMatches(
        string userId,
        ICollection<Sports> preferredSports,
        Location? baseLocation,
        string? q,
        int page = 1,
        int pageSize = 5,
        CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .Include(m => m.Participants)
            .Include(m => m.Creator)
            .Where(m =>
                m.CreatorId != userId && m.Participants.Count < m.maxPlayers &&
                (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent)
            );

        if (!string.IsNullOrEmpty(q)) { query = query.Where(m => m.Description.ToLower().Contains(q.ToLower())); }

        List<Match> matches = await query.ToListAsync(ct);

        var ranked = matches
            .Select(m => new
            {
                Match = m,
                HasPreferredSport = preferredSports.Contains(m.Sport),
                Distance = baseLocation is null
                    ? 0
                    : HaversineDistance(
                        baseLocation.Latitude,
                        baseLocation.Longitude,
                        m.Location.Latitude,
                        m.Location.Longitude
                    )
            })
            .OrderByDescending(x => x.HasPreferredSport)
            .ThenBy(x => x.Distance)
            .Take(pageSize)
            .Select(x => x.Match)
            .ToList();

        var dtos = ranked.Select(m => m.ToDto()).ToList();

        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = ranked.Count,
                NextPageAvailable = false,
                PreviousPageAvailable = false
            }
        );
    }
}