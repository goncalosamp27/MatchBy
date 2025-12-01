using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Enums;
using MatchBy.Services.Conversations;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using IEmailSender = MatchBy.Services.Email.IEmailSender;
using OrderBy = MatchBy.DTOs.Match.OrderBy;
using SortBy = MatchBy.DTOs.Match.SortBy;

namespace MatchBy.Services.Matches;

public class MatchesService(
    IConversationService conversationService,
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
    private static async Task<Result<PaginationResponse<List<MatchDto>>>> PaginateMatchesAsync(IQueryable<Match> query, string? q, int page = 1, int pageSize = 5, double userLatitude = 0, double userLongitude = 0,
        SortBy sortBy = SortBy.MatchDateTime, OrderBy orderBy = OrderBy.Ascending, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(m => m.Description.ToLower().Contains(q.ToLower()));
        }
        
        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);
        
        List<Match> matches = [];
        switch (sortBy)
        {
            case SortBy.MatchDateTime when orderBy == OrderBy.Ascending:
                matches = await query
                .OrderBy(m => m.MatchDateTimeUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
                break;
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
                    .OrderBy(m => HaversineDistance(
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
                    .OrderByDescending(m => HaversineDistance(
                        m.Location.Latitude,
                        m.Location.Longitude,
                        userLatitude,
                        userLongitude))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                break;
        }

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

    public async Task<Result<List<string>>> GetAllMatchCountries(CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        return Result<List<string>>.Ok(
            await dbContext
                .Matches
                .AsNoTracking()
                .Select(m => m.Location.Country)
                .Distinct()
                .Where(c => !string.IsNullOrEmpty(c))
                .OrderBy(c => c)
                .ToListAsync(ct)
        );
    }

    public async Task<Result<List<string>>> GetAllCitiesByCountry(string country, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        return Result<List<string>>.Ok(
            await dbContext
                .Matches
                .AsNoTracking()
                .Where(m => m.Location.Country.ToLower() == country.ToLower())
                .Select(m => m.Location.City)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct)
        );
    }

    /// <summary>
    /// Retrieves a paginated list of matches with optional filtering by status, search query, and user access.
    /// </summary>
    /// <param name="matchQueryParametersDto">DTO containing all query parameters for filtering matches.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with filtered match DTOs, or an error if failed to retrieve user invites.
    /// </returns>
    public async Task<Result<PaginationResponse<List<MatchDto>>>> GetMatches(MatchQueryParametersDto matchQueryParametersDto, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Match> query = dbContext
            .Matches
            .AsNoTracking()
            .Include(m => m.Participants)
            .Include(m => m.Creator);

        if (!string.IsNullOrEmpty(matchQueryParametersDto.UserId))
        {
            Result<PaginationResponse<List<MatchInviteDto>>> receivedUserInvitesResult = 
                await matchInvitesService.GetReceivedInvites(matchQueryParametersDto.UserId, 1, int.MaxValue, ct);
    
            if(!receivedUserInvitesResult.Success)
            {
                return Result<PaginationResponse<List<MatchDto>>>.Fail(receivedUserInvitesResult.ErrorMessages[0]);
            }
    
            var invitedMatchIds = receivedUserInvitesResult.Data!.Data
                .Where(i => i.Status == InviteStatus.Pending)
                .Select(i => i.MatchId)
                .ToList(); 
    
            query = query.Where(m =>
                m.CreatorId == matchQueryParametersDto.UserId || 
                m.Participants.Any(p => p.Id == matchQueryParametersDto.UserId) || 
                m.Privacy == MatchPrivacy.Public ||
                invitedMatchIds.Contains(m.Id));
        }
        else
        {
            query = query.Where(m => m.Privacy == MatchPrivacy.Public);   
        }

        if (matchQueryParametersDto.SportsList.Any())
        {
            query = query.Where(m => matchQueryParametersDto.SportsList.Contains(m.Sport));
        }
        
        if(!string.IsNullOrEmpty(matchQueryParametersDto.Country))
        {
            query = query.Where(m => m.Location.Country.ToLower() == matchQueryParametersDto.Country.ToLower());
        }
        
        if(!string.IsNullOrEmpty(matchQueryParametersDto.City))
        {
            query = query.Where(m => m.Location.City.ToLower() == matchQueryParametersDto.City.ToLower());
        }
        
        if (matchQueryParametersDto.FromDateUtc.HasValue)
        {
            query = query.Where(m => DateOnly.FromDateTime(m.MatchDateTimeUtc) >= matchQueryParametersDto.FromDateUtc.Value);
        }
        
        if( matchQueryParametersDto.ToDateUtc.HasValue)
        {
            query = query.Where(m => DateOnly.FromDateTime(m.MatchDateTimeUtc) <= matchQueryParametersDto.ToDateUtc.Value);
        }
        
        if( matchQueryParametersDto.FromTimeUtc.HasValue)
        {
            query = query.Where(m => m.MatchDateTimeUtc.TimeOfDay >= TimeSpan.FromHours(matchQueryParametersDto.FromTimeUtc.Value));
        }
        
        if( matchQueryParametersDto.ToTimeUtc.HasValue)
        {
            query = query.Where(m => m.MatchDateTimeUtc.TimeOfDay <= TimeSpan.FromHours(matchQueryParametersDto.ToTimeUtc.Value));
        }
        
        if(matchQueryParametersDto.MinimumPlayersAverage != MinimumPlayersAverage.All)
        {
            int minAverage = (int)matchQueryParametersDto.MinimumPlayersAverage;
            query = query.Where(m => m.Participants.Average(p => p.Rating) >= minAverage);
        }

        if (matchQueryParametersDto is { MaxDistanceInKm: not null, UserLatitude: not null, UserLongitude: not null })
        {
            double userLat = matchQueryParametersDto.UserLatitude.Value;
            double userLon = matchQueryParametersDto.UserLongitude.Value;
            int maxDistance = matchQueryParametersDto.MaxDistanceInKm.Value;

            const double R = 6371;
            query = query.Where(m =>
                Math.Acos(
                    Math.Sin(m.Location.Latitude * Math.PI / 180) *
                    Math.Sin(userLat * Math.PI / 180) +
                    Math.Cos(m.Location.Latitude * Math.PI / 180) *
                    Math.Cos(userLat * Math.PI / 180) *
                    Math.Cos((m.Location.Longitude - userLon) * Math.PI / 180)
                ) * R <= maxDistance
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

            match = await dbContext
                .Matches
                .Include(m => m.Participants)
                .Include(m => m.Creator)
                .Where(m => m.Participants.Any(p => p.Id == userId) || m.CreatorId == userId ||
                            m.Privacy == MatchPrivacy.Public || (matchInvite.Success && matchInvite.Data!.Status == InviteStatus.Pending))
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
    /// A result containing the created match DTO if the match was successfully created, or an error message if validation fails.
    /// </returns>
    /// <remarks>
    /// The creator is automatically added as the first participant in the match.
    /// </remarks>
    public async Task<Result<MatchDto>> CreateMatch(CreateMatchDto createMatchDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createMatchValidator.ValidateAsync(createMatchDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<MatchDto>.Fail(validationResult.ToString());
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match match = createMatchDto.ToEntity();
        match.Participants = (List<ApplicationUser>)
            [await dbContext.Users.FirstAsync(u => u.Id == createMatchDto.CreatorId, ct)];
        await dbContext.Matches.AddAsync(match, ct);
        await dbContext.SaveChangesAsync(ct);
        
        var conversationCreationDto = new CreateConversationDto
        {
            CreatorUserId = createMatchDto.CreatorId,
            ConversationType = ConversationType.Match,
            Title = createMatchDto.Sport + " Match",
            ParticipantIds = [createMatchDto.CreatorId],
            MatchId = match.Id
        };

        Result<ConversationDto> conversationResult =
            await conversationService.CreateConversationAsync(conversationCreationDto, ct);

        if (!conversationResult.Success)
        {
            return Result<MatchDto>.Fail("Failed to create conversation for the match: " + string.Join(", ", conversationResult.ErrorMessages));
        }

        match.ConversationId = conversationResult.Data!.Id; // since we set the conversation's ID to be the same as the match's ID

        await dbContext.SaveChangesAsync(ct);

        // Send invites
        var membersToInvite = createMatchDto.MembersIds.Where(m => m != createMatchDto.CreatorId).Distinct().ToList();
        foreach (string receiverId in membersToInvite)
        {
            await matchInvitesService.CreateInvite(new CreateMatchInviteDto
            {
                MatchId = match.Id,
                SenderId = createMatchDto.CreatorId,
                ReceiverId = receiverId,
                Content = $"You've been invited to join a {match.Sport} match!"
            }, ct);
        }

        return Result<MatchDto>.Ok(match.ToDto());
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

        //get invite if exists
        Result<MatchInviteDto> matchInvite = await matchInvitesService.GetMatchInvite(matchId, userId, ct);

        Match? match = await dbContext
            .Matches
            .Include(m => m.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Where(m => m.Privacy == MatchPrivacy.Public || matchInvite.Success)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found.");
        }

        if (match.Status != MatchStatus.Pendent)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} is not open for joining.");
        }

        ApplicationUser? user = await dbContext
            .Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }
        
        if(user.Rating < (int)match.MinimumPlayersRating)
        {
            return Result<MatchDto>.Fail($"User with id {userId} does not meet the minimum player rating requirement.");
        }
        
        
        // If he has an invite, accept it, if not, just join if public
        if (matchInvite.Success)
        {
            Result<MatchInviteDto> aceptInviteResult = await matchInvitesService.AcceptInvite(matchInvite.Data!.Id, userId, ct);
            if(!aceptInviteResult.Success)
            {
                return Result<MatchDto>.Fail(aceptInviteResult.ErrorMessages[0]);
            }
        }
        else if(match.Privacy == MatchPrivacy.Public)
        {
            match.Participants.Add(user);
            match.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        
        Conversation? conversation = match.Conversation;

        if (conversation is not null)
        {
            conversation.Participants.Add(user);
            conversation.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            return Result<MatchDto>.Fail("Associated conversation not found.");
        }

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
    public async Task<Result<MatchDto>> ConfirmMatch(string matchId, string userId, CancellationToken ct = default)
    {        
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Match? match = await dbContext
            .Matches
            .Include(m => m.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Where(m => m.CreatorId == userId)
            .FirstOrDefaultAsync(m => m.Id == matchId, ct);

        if (match is null)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found.");
        }

        if (match.Status != MatchStatus.Pendent)
        {
            return Result<MatchDto>.Fail($"Cant confirm match with id {matchId} because its not in pendent status.");
        }
        
        if(match.Participants.Count < match.MinPlayers)
        {
            return Result<MatchDto>.Fail($"Cant confirm match with id {matchId} because it doesnt have the required number of players.");
        }

        match.Status = MatchStatus.Confirmed;
        match.UpdatedAtUtc = DateTime.UtcNow;
        
        await dbContext.SaveChangesAsync(ct);

        foreach (ApplicationUser participant in match.Participants.Where(p => p.Id != userId))
        {
            var notification = new CreateNotificationDto
            {
                Type = NotificationType.MatchReminder,
                ReceiverUserId = participant.Id,
                SenderUserId = userId,
                RelatedEntityId = match.Id,
                RelatedEntityName = $"{match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm}",
                Title = "Match Confirmed",
                Message = $"The match {match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm} has been confirmed!",
                ActionUrl = $"/matches/{match.Id}"
            };

            await notificationService.SendNotificationAsync(notification, ct);
        }
        
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
            match.UpdatedAtUtc = DateTime.UtcNow;
            
            // Send cancellation emails to all participants
            foreach (ApplicationUser participant in match.Participants.Where(p => p.Id != userId))
            {
                CreateNotificationDto notification = new()
                {
                    Type = NotificationType.MatchReminder,
                    ReceiverUserId = participant.Id,
                    SenderUserId = userId,
                    RelatedEntityId = match.Id,
                    RelatedEntityName = $"{match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm}",
                    Title = "Match Cancelled",
                    Message = $"The match {match.Sport} in {match.MatchDateTimeUtc:dd/MM/yyyy HH:mm} has been cancelled by the creator.",
                    ActionUrl = $"/matches/{match.Id}"
                };
                await notificationService.SendNotificationAsync(notification, ct);
                
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
            
            /*Result<bool> deleteConversation =
                await conversationService.DeleteConversationAsync(match.ConversationId!, match.CreatorId, ct);
            if (!deleteConversation.Success)
            {
                return Result<bool>.Fail("Failed to delete the associated conversation: " +
                                        deleteConversation.ErrorMessages[0]);
            }
            
            match.Participants.Clear();
            */
        }
        else
        {
            Conversation? conversation = await dbContext
                .Conversations
                .Include(c => c.Participants)
                .Where(c => c.Id == match.ConversationId)
                .FirstOrDefaultAsync(ct);

            if (conversation is not null)
            {
                conversation.Participants.Remove(user);
                conversation.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                return Result<bool>.Fail("Associated conversation not found.");
            }
            
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
            .Where(m => m.CreatorId == userId && m.Participants.Count < m.MaxPlayers && (m.Status == MatchStatus.Confirmed || m.Status == MatchStatus.Pendent));

        return await PaginateMatchesAsync(query, q, page, pageSize, ct: ct);
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
        
        return await PaginateMatchesAsync(query, q, page, pageSize, ct: ct);
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

        return await PaginateMatchesAsync(query, q, page, pageSize, ct: ct);
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
                m.CreatorId != userId && m.Participants.Count < m.MaxPlayers &&
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