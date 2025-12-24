using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Models;
using MatchBy.Enums;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.Friend;
using MatchBy.Repositories.Match;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using IEmailSender = MatchBy.Services.Email.IEmailSender;

namespace MatchBy.Services.Matches;

public class MatchesService(
    IFriendRepository friendRepository,
    IUserRepository userRepository,
    IImageRefreshService imageRefreshService,
    IConversationRepository conversationRepository,
    IMatchRepository matchesRepository,
    IConversationService conversationService,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateMatchDto> createMatchValidator,
    IValidator<UpdateMatchDto> updateMatchValidator, 
    IMatchesInvitesService matchInvitesService,
    IEmailSender emailSender,
    INotificationService notificationService) : IMatchesService
{
    public async Task<Result<List<string>>> GetAllMatchCountries(CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        List<string> countries = await matchesRepository.GetAllMatchCountries(dbContext, ct);

        return Result<List<string>>.Ok(countries);
    }

    public async Task<Result<List<string>>> GetAllCitiesByCountry(string country, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        List<string> cities = await matchesRepository.GetAllCitiesByCountry(country, dbContext, ct);

        return Result<List<string>>.Ok(cities);
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
        
        List<string> invitedMatchIds = [];
        
        if (!string.IsNullOrEmpty(matchQueryParametersDto.UserId))
        {
            Result<PaginationResponse<List<MatchInviteDto>>> receivedUserInvitesResult = await matchInvitesService.GetReceivedInvites(matchQueryParametersDto.UserId, 1, int.MaxValue, ct);
    
            if(!receivedUserInvitesResult.Success)
            {
                return Result<PaginationResponse<List<MatchDto>>>.Fail(receivedUserInvitesResult.ErrorMessages[0]);
            }
    
            invitedMatchIds = receivedUserInvitesResult.Data.Data
                .Where(i => i.Status == InviteStatus.Pending)
                .Select(i => i.MatchId)
                .ToList(); 
        }
        
        PaginationResponse<List<Match>> matches = await matchesRepository.GetMatches(matchQueryParametersDto, invitedMatchIds, dbContext, ct);
        
        foreach (Match match in matches.Data)
        {
            await imageRefreshService.RefreshUserProfileImageAsync(match.Creator!);
            foreach (ApplicationUser member in match.Participants)
            {
                await imageRefreshService.RefreshUserProfileImageAsync(member);
            }
        }
        
        await dbContext.SaveChangesAsync(ct);
        
        var matchDtos = matches.Data.Select(m => m.ToDto()).ToList();
        
        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = matchDtos,
                Page = matches.Page,
                PageSize = matches.PageSize,
                TotalCount = matches.TotalCount
            });
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

        bool hasPendingInvite = false;
        if (!string.IsNullOrEmpty(userId))
        {
            Result<MatchInviteDto> matchInvite = await matchInvitesService.GetMatchInvite(matchId, userId, ct);
            if (matchInvite.Success)
            {
                hasPendingInvite = matchInvite.Data.Status == InviteStatus.Pending;
            }
        }
        
        Match? match = await matchesRepository.GetByIdAsync(matchId, userId, hasPendingInvite, dbContext, ct);
        
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

        ApplicationUser? creator = await userRepository.GetByIdAsync(createMatchDto.CreatorId, dbContext, ct);

        if (creator is null)
        {
            return Result<MatchDto>.Fail($"Creator with id {createMatchDto.CreatorId} not found.");
        }

        Match match = createMatchDto.ToEntity();
        match.Participants = [creator];
        matchesRepository.Add(match, dbContext);
        
        await dbContext.SaveChangesAsync(ct);
        
        var conversationCreationDto = new CreateConversationDto
        {
            CreatorUserId = createMatchDto.CreatorId,
            ConversationType = ConversationType.Match,
            Title = createMatchDto.Sport + " Match",
            ParticipantIds = [createMatchDto.CreatorId],
            MatchId = match.Id
        };

        Result<ConversationDto> conversationResult = await conversationService.CreateConversationAsync(conversationCreationDto, ct);
        if (!conversationResult.Success)
        {
            return Result<MatchDto>.Fail("Failed to create conversation for the match: " + string.Join(", ", conversationResult.ErrorMessages));
        }
        
        if (match.Privacy == MatchPrivacy.Public)
        {
            PaginationResponse<List<Friend>> friendships = await friendRepository.GetUserFriends(creator.Id, dbContext,1, 1000, ct);

            if (friendships.TotalCount > 0)
            {
                string matchName = $"{match.Sport} on {match.MatchDateTimeUtc:dd/MM/yyyy 'at' HH:mm}";

                var friendIds = friendships.Data
                    .Select(f => f.SenderId == creator.Id ? f.ReceiverId : f.SenderId)
                    .Distinct()
                    .ToList();

                foreach (string friendId in friendIds)
                {
                    var notification = new CreateNotificationDto
                    {
                        Type = NotificationType.Match,
                        ReceiverUserId = friendId,
                        SenderUserId = creator.Id,
                        RelatedEntityId = match.Id,
                        RelatedEntityName = matchName,
                        Title = "Your friend created a new match",
                        Message = $"{creator.DisplayName} created a new match: {matchName}",
                        ActionUrl = $"/matches/{match.Id}"
                    };

                    await notificationService.SendNotificationAsync(notification, ct);
                }
            }
        }

        match.ConversationId = conversationResult.Data.Id; // since we set the conversation's ID to be the same as the match's ID

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
        Match? match = await matchesRepository.GetByIdAsync(matchId, userId, false, dbContext, ct);
        if (match is null || match.CreatorId != userId)
        {
            return Result<bool>.Fail(
                $"Match with id {matchId} not found or user {userId} is not the creator.");
        }

        matchesRepository.Remove(match, dbContext);
        if (match.ConversationId != null)
        {
            conversationRepository.Remove(match.Conversation!, dbContext);
        }
        
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
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
        if (!matchInvite.Success)
        {
            return Result<MatchDto>.Fail(matchInvite.ErrorMessages[0]);
        }
        
        Match? match = await matchesRepository.GetByIdAsync(matchId, userId, matchInvite.Data.Status == InviteStatus.Pending, dbContext, ct);
        if (match is null)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found.");
        }

        if (match.Status != MatchStatus.Pendent)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} is not open for joining.");
        }

        ApplicationUser? user = await userRepository.GetByIdAsync(userId, dbContext, ct);
        if (user is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }
        
        if(user.Rating < (int)match.MinimumPlayersRating)
        {
            return Result<MatchDto>.Fail($"User with id {userId} does not meet the minimum player rating requirement.");
        }
        
        // If he has an invite, accept it, if not, just join if public
        if (matchInvite.Success && matchInvite.Data.Status == InviteStatus.Pending)
        {
            Result<MatchInviteDto> aceptInviteResult = await matchInvitesService.AcceptInvite(matchInvite.Data.Id, userId, ct);
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

        string matchName = $"{match.Sport} on {match.MatchDateTimeUtc:dd/MM/yyyy 'at' HH:mm}";
        
        ApplicationUser? newParticipant = await userRepository.GetByIdAsync(userId, dbContext, ct);
        if (newParticipant is null)
        {
            return Result<MatchDto>.Fail($"User with id {userId} not found.");
        }
        
        var notification = new CreateNotificationDto
        {
            Type = NotificationType.Match,
            ReceiverUserId = match.CreatorId,
            SenderUserId = userId,
            RelatedEntityId = match.Id,
            RelatedEntityName = matchName,
            Title = "New participant",
            Message = $"{newParticipant.DisplayName} joined the match {matchName}",
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

        Match? match = await matchesRepository.GetByIdAsync(matchId, userId, false, dbContext, ct);
        if (match is null || match.CreatorId != userId)
        {
            return Result<MatchDto>.Fail($"Match with id {matchId} not found or user {userId} is not the creator.");
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
                Type = NotificationType.Match,
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

        ApplicationUser? user = await userRepository.GetByIdAsync(userId, dbContext, ct);
        if (user is null)
        {
            return Result<bool>.Fail($"User with id {userId} not found.");
        }
        
        Match? match = await matchesRepository.GetByIdAsync(matchId, userId, false, dbContext, ct);
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
                    Type = NotificationType.Match,
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
                    await Task.Delay(100, ct);
                }
            }
            
            Result<bool> deleteConversation = await conversationService.DeleteConversationAsync(match.ConversationId!, match.CreatorId, ct);
            if (!deleteConversation.Success)
            {
                return Result<bool>.Fail("Failed to delete the associated conversation: " +
                                        deleteConversation.ErrorMessages[0]);
            }
        }
        else
        {
            Conversation? conversation = await conversationRepository.GetByIdAsync(match.ConversationId!, user.Id, dbContext, ct);
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
            await dbContext.SaveChangesAsync(ct);
        }
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

        PaginationResponse<List<Match>> matchesForUser = await matchesRepository.GetMatchesForUser(userId, dbContext, q, page, pageSize, ct);
        
        foreach (Match match in matchesForUser.Data)
        {
            await imageRefreshService.RefreshUserProfileImageAsync(match.Creator!);
            foreach (ApplicationUser member in match.Participants)
            {
                await imageRefreshService.RefreshUserProfileImageAsync(member);
            }
        }
        
        await dbContext.SaveChangesAsync(ct);

        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = [..matchesForUser.Data.Select(m => m.ToDto())],
                Page = matchesForUser.Page,
                PageSize = matchesForUser.PageSize,
                TotalCount = matchesForUser.TotalCount
            });
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

        PaginationResponse<List<Match>> matchesForUser = await matchesRepository.GetMatchesExceptUser(userId, dbContext, q, page, pageSize, ct);
        
        foreach (Match match in matchesForUser.Data)
        {
            await imageRefreshService.RefreshUserProfileImageAsync(match.Creator!);
            foreach (ApplicationUser member in match.Participants)
            {
                await imageRefreshService.RefreshUserProfileImageAsync(member);
            }
        }
        
        await dbContext.SaveChangesAsync(ct);

        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = [..matchesForUser.Data.Select(m => m.ToDto())],
                Page = matchesForUser.Page,
                PageSize = matchesForUser.PageSize,
                TotalCount = matchesForUser.TotalCount
            });
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

        PaginationResponse<List<Match>> matchesForUser = await matchesRepository.GetMatchesUserAttending(userId, dbContext, q, page, pageSize, ct);
        
        foreach (Match match in matchesForUser.Data)
        {
            await imageRefreshService.RefreshUserProfileImageAsync(match.Creator!);
            foreach (ApplicationUser member in match.Participants)
            {
                await imageRefreshService.RefreshUserProfileImageAsync(member);
            }
        }
        
        await dbContext.SaveChangesAsync(ct);

        return Result<PaginationResponse<List<MatchDto>>>.Ok(
            new PaginationResponse<List<MatchDto>>
            {
                Data = [..matchesForUser.Data.Select(m => m.ToDto())],
                Page = matchesForUser.Page,
                PageSize = matchesForUser.PageSize,
                TotalCount = matchesForUser.TotalCount
            });
    }
    
}