using Amazon.S3;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.DTOs.Notification;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Repositories.ChatConversation;
using MatchBy.Repositories.Team;
using MatchBy.Repositories.TeamInvite;
using MatchBy.Repositories.User;
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Notifications;
using MatchBy.Services.S3;
using MatchBy.Services.TeamInvites;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Teams;

public class TeamService(
    ITeamInviteRepository teamInviteRepository,
    ITeamRepository teamRepository,
    IUserRepository userRepository,
    IConversationRepository conversationRepository,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IS3Service s3Service,
    IConversationService conversationService,
    ITeamsInvitesService teamInvitesService,
    IValidator<CreateTeamDto> createTeamValidator,
    IValidator<UpdateTeamDto> updateTeamValidator,
    IImageRefreshService imageRefreshService,
    INotificationService notificationService) : ITeamService
{
    /// <summary>
    /// Retrieves a paginated list of teams based on query parameters, including teams the user has pending invites for.
    /// </summary>
    /// <param name="teamQueryParametersDto">DTO containing filter parameters (sort, order, privacy, query, pagination, userId).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with filtered and sorted team DTOs.
    /// Private teams are included if the user is a member or has a pending invite.
    /// </returns>
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PaginationResponse<List<TeamInvite>> invitedTeamIds = await teamInviteRepository.GetInvites(
            teamQueryParametersDto.UserId,
            dbContext,
            1,
            int.MaxValue-1,
            ct);
        
        PaginationResponse<List<Team>> teams =
            await teamRepository.GetTeamsAsync(teamQueryParametersDto, invitedTeamIds.Data.Select(i => i.TeamId).Distinct().ToList(), dbContext, ct);

        // Refresh images in parallel for better performance
        var distinctTeams = teams.Data.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctTeams.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = teams.Data.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        await dbContext.SaveChangesAsync(ct);

        var list = teams.Data.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(new PaginationResponse<List<TeamDto>>
        {
            Data = list,
            TotalCount = teams.TotalCount,
            Page = teams.Page,
            PageSize = teams.PageSize
        });
    }

    /// <summary>
    /// Retrieves a paginated list of teams available for the user to join (excluding teams they own or are members of).
    /// </summary>
    /// <param name="teamQueryParametersDto">DTO containing filter parameters (sort, order, privacy, query, pagination, userId).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with available team DTOs.
    /// Only includes teams where the user is not the owner and not already a member.
    /// </returns>
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetAvailableTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PaginationResponse<List<TeamInvite>> invitedTeamIds = await teamInviteRepository.GetInvites(
            teamQueryParametersDto.UserId,
            dbContext,
            1,
            int.MaxValue - 1,
            ct);

        PaginationResponse<List<Team>> availableTeams =
            await teamRepository.GetAvailableTeamsAsync(teamQueryParametersDto, invitedTeamIds.Data.Select(i => i.TeamId).Distinct().ToList(), dbContext, ct);

        // Refresh images in parallel for better performance
        var distinctTeams = availableTeams.Data.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctTeams.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = availableTeams.Data.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        await dbContext.SaveChangesAsync(ct);

        var list = availableTeams.Data.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(new PaginationResponse<List<TeamDto>>
        {
            Data = list,
            TotalCount = availableTeams.TotalCount,
            Page = availableTeams.Page,
            PageSize = availableTeams.PageSize
        });
    }

    /// <summary>
    /// Retrieves a paginated list of teams that the specified user owns.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of teams per page.</param>
    /// <param name="q">Optional search query to filter teams by name or description (case-insensitive).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with team DTOs owned by the user, or an error if userId is null or empty.
    /// </returns>
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserOwnAsync(
        string userId, int page, int pageSize, string q, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        PaginationResponse<List<Team>> ownedTeams =
            await teamRepository.GetTeamsUserOwnAsync(userId, page, pageSize, q, dbContext, ct);

        // Refresh images in parallel for better performance
        var distinctTeams = ownedTeams.Data.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctTeams.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = ownedTeams.Data.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        await dbContext.SaveChangesAsync(ct);

        var list = ownedTeams.Data.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(new PaginationResponse<List<TeamDto>>
        {
            Data = list,
            TotalCount = ownedTeams.TotalCount,
            Page = ownedTeams.Page,
            PageSize = ownedTeams.PageSize
        });
    }

    /// <summary>
    /// Retrieves a paginated list of teams that the specified user participates in as a member (but not as owner).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="page">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of teams per page.</param>
    /// <param name="q">Optional search query to filter teams by name or description (case-insensitive).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with team DTOs the user participates in, or an error if userId is null or empty.
    /// </returns>
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserParticipateAsync(
        string userId, int page, int pageSize, string q, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PaginationResponse<List<TeamDto>>>.Fail("User ID cannot be null or empty.");
        }

        PaginationResponse<List<Team>> participatingTeams =
            await teamRepository.GetTeamsUserOwnAsync(userId, page, pageSize, q, dbContext, ct);

        // Refresh images in parallel for better performance
        var distinctTeams = participatingTeams.Data.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctTeams.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = participatingTeams.Data.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        await dbContext.SaveChangesAsync(ct);

        var list = participatingTeams.Data.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(new PaginationResponse<List<TeamDto>>
        {
            Data = list,
            TotalCount = participatingTeams.TotalCount,
            Page = participatingTeams.Page,
            PageSize = participatingTeams.PageSize
        });
    }

    /// <summary>
    /// Retrieves a specific team by its unique identifier, with access control based on privacy settings and invites.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to retrieve.</param>
    /// <param name="userId">The unique identifier of the user requesting the team.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the team DTO if found and accessible, or an error message if:
    /// - The team is not found
    /// - Access is denied (private team and user is not a member or doesn't have a pending invite)
    /// - Failed to retrieve team invites
    /// </returns>
    /// <remarks>
    /// Public teams are accessible to everyone. Private teams are only accessible if the user is a member
    /// or has a pending invite. Images are refreshed in parallel for better performance.
    /// </remarks>
    public async Task<Result<TeamDto>> GetTeamByIdAsync(string teamId, string userId, CancellationToken ct = default)
    {
        Result<PaginationResponse<List<TeamInviteDto>>> invitesResult =
            await teamInvitesService.GetInvites(teamId, 1, int.MaxValue, ct);

        if (!invitesResult.Success)
        {
            return Result<TeamDto>.Fail("Failed to retrieve team invites.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        bool hasInvite = invitesResult.Data.Data.Where(i => i is { Status: InviteStatus.Pending, IsExpired: false })
            .Any(i => i.ReceiverId == userId);

        Team? team = await teamRepository.GetByIdAsync(teamId, userId, hasInvite, dbContext, ct);
        if (team is null)
        {
            return Result<TeamDto>.Fail("Team not found or access denied.");
        }

        // Refresh images in parallel for better performance
        IEnumerable<Task> userImageTasks = team.Members.Select(imageRefreshService.RefreshUserProfileImageAsync);
        Task teamImageTask = imageRefreshService.RefreshTeamImagesAsync(team);
        await Task.WhenAll(userImageTasks.Append(teamImageTask));

        return Result<TeamDto>.Ok(team.ToDto());
    }

    /// <summary>
    /// Creates a new team with the specified details, creates an associated conversation, and sends invites to selected members.
    /// </summary>
    /// <param name="createTeamDto">DTO containing team creation details (name, description, privacy, owner, members, image).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the created team DTO if successful, or an error message if:
    /// - Validation fails
    /// - Owner user is not found
    /// - Failed to create associated conversation
    /// - Failed to upload team image (if provided)
    /// </returns>
    /// <remarks>
    /// This method creates the team, adds the owner as the first member, creates an associated team conversation,
    /// and sends invites to all members specified in MembersIds (excluding the owner). If an image is provided,
    /// it is uploaded to S3 storage.
    /// </remarks>
    public async Task<Result<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createTeamValidator.ValidateAsync(createTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team team = createTeamDto.ToEntity();

        ApplicationUser? owner = await userRepository.GetByIdAsync(createTeamDto.OwnerId, dbContext, ct);
        if (owner is null)
        {
            return Result<TeamDto>.Fail("User not found.");
        }

        // Don't add members directly - they will be added via invites
        team.Members = [owner];
        team.ConversationId = null; // will be set after conversation is created, to not give errors on FK constraint

        teamRepository.Add(team, dbContext);
        await dbContext.SaveChangesAsync(ct);

        var conversationCreationDto = new CreateConversationDto
        {
            CreatorUserId = team.OwnerId,
            ConversationType = ConversationType.Team,
            Title = team.Name,
            ParticipantIds = [createTeamDto.OwnerId],
            TeamId = team.Id
        };

        Result<ConversationDto> conversationResult =
            await conversationService.CreateConversationAsync(conversationCreationDto, ct);
        if (!conversationResult.Success)
        {
            return Result<TeamDto>.Fail("Failed to create associated conversation: " +
                                        conversationResult.ErrorMessages[0]);
        }

        team.ConversationId =
            conversationResult.Data.Id; // since we set the conversation's ID to be the same as the team's ID

        await dbContext.SaveChangesAsync(ct);

        // Send invites to selected users (excluding the owner)
        var membersToInvite = createTeamDto.MembersIds.Where(m => m != createTeamDto.OwnerId).ToList();
        if (!membersToInvite.Any())
        {
            return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
        }

        foreach (string receiverId in membersToInvite)
        {
            Result<TeamInviteDto> result = await teamInvitesService.CreateInvite(new CreateTeamInviteDto
            {
                TeamId = team.Id,
                SenderId = createTeamDto.OwnerId,
                ReceiverId = receiverId,
                Content = $"You've been invited to join {team.Name}!"
            }, ct);
            Console.WriteLine(result.Success
                ? $"Invite sent to user {receiverId} for team {team.Id}."
                : $"Failed to send invite to user {receiverId} for team {team.Id}: {string.Join(", ", result.ErrorMessages)}");
        }

        if (createTeamDto.File is not null)
        {
            Result<bool> updateTeamImageResult = await UpdateTeamImageAsync(team, createTeamDto.File);
            if (!updateTeamImageResult.Success)
            {
                return Result<TeamDto>.Fail("Failed to upload team image: " +
                                            string.Join(", ", updateTeamImageResult.ErrorMessages));
            }
        }

        await dbContext.SaveChangesAsync(ct);

        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    /// <summary>
    /// Updates the team's image by uploading it to S3 storage and generating a presigned URL.
    /// </summary>
    /// <param name="team">The team entity to update the image for.</param>
    /// <param name="file">The browser file containing the image to upload.</param>
    /// <returns>
    /// A result containing the updated team DTO if successful, or an error message if:
    /// - Failed to upload the image to S3
    /// - Failed to generate presigned URL
    /// </returns>
    /// <remarks>
    /// This method uploads the image, generates a presigned URL (valid for 30 minutes), deletes the previous image
    /// if it exists, and updates the team's image metadata in the database.
    /// </remarks>
    private async Task<Result<bool>> UpdateTeamImageAsync(
        Team team,
        IBrowserFile file)
    {
        // upload
        Result<string> uploadedKey = await s3Service.UploadBrowserFileAsync(file, $"teams/{team.Id}/image");
        if (!uploadedKey.Success)
        {
            return Result<bool>.Fail(uploadedKey.ErrorMessages.ToArray());
        }

        // URL presign
        Result<string> url =
            await s3Service.GetPresignedUrlAsync($"teams/{team.Id}/image/{uploadedKey.Data}", HttpVerb.GET);
        if (!url.Success)
        {
            return Result<bool>.Fail(url.ErrorMessages.ToArray());
        }

        // delete previous, if it exists
        string? oldKey = team.Image?.Key;
        if (!string.IsNullOrWhiteSpace(oldKey) && !oldKey.Equals(uploadedKey.Data, StringComparison.OrdinalIgnoreCase))
        {
            await s3Service.DeleteFileAsync($"teams/{team.Id}/image/{oldKey}");
        }

        // store the image info
        team.Image = new FileStore(
            Url: url.Data,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey.Data,
            FileCategory: FileCategory.TeamImage,
            FileType: FileType.Image,
            CreatedAtUtc: DateTime.UtcNow
        );
        team.UpdatedAtUtc = DateTime.UtcNow;

        return Result<bool>.Ok(true);
    }

    /// <summary>
    /// Updates an existing team's details. Only the team owner can update the team.
    /// </summary>
    /// <param name="updateTeamDto">DTO containing the team update details (id, name, description, privacy, owner, members, image).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the updated team DTO if successful, or an error message if:
    /// - Validation fails
    /// - Team not found or user is not the owner
    /// - Associated conversation not found
    /// - Failed to update team image (if provided)
    /// </returns>
    /// <remarks>
    /// This method updates the team's name, description, privacy, and removes members that are no longer in the list.
    /// New members should be added via invites, not directly through this method. The associated conversation title
    /// is also updated to match the team name. If an image is provided, it is uploaded to S3 storage.
    /// </remarks>
    public async Task<Result<TeamDto>> UpdateTeamAsync(UpdateTeamDto updateTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateTeamValidator.ValidateAsync(updateTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // only the creator can update participants
        Team? team =
            await teamRepository.GetTeamUserOwnsByIdAsync(updateTeamDto.Id, updateTeamDto.OwnerId, dbContext, ct);
        if (team is null)
        {
            return Result<TeamDto>.Fail("Team not found or user is not the creator.");
        }

        // Get current member IDs
        var currentMemberIds = team.Members.Select(m => m.Id).ToHashSet();
        var newMemberIds = updateTeamDto.MembersIds.ToHashSet();

        // Find users to remove (members that are no longer in the list)
        var usersToRemove = currentMemberIds.Except(newMemberIds).ToList();
        if (usersToRemove.Any())
        {
            var membersToRemove = team.Members.Where(m => usersToRemove.Contains(m.Id)).ToList();
            foreach (ApplicationUser member in membersToRemove)
            {
                team.Members.Remove(member);
            }
        }

        // Note: New members will be added via invites, not directly here
        // The MembersIds in updateTeamDto should only contain current members

        team.UpdateEntity(updateTeamDto); // updates name, description, privacy and updatedAtUtc

        Conversation? conversation =
            await conversationRepository.GetByIdAsync(team.ConversationId!, team.OwnerId, dbContext, ct);
        if (conversation is not null)
        {
            conversation.Title = team.Name;
            conversation.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            return Result<TeamDto>.Fail("Associated conversation not found.");
        }

        if (updateTeamDto.File is not null)
        {
            await UpdateTeamImageAsync(team, updateTeamDto.File);
        }

        await dbContext.SaveChangesAsync(ct);
        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    /// <summary>
    /// Soft deletes a team and its associated conversation and pending invites. Only the team owner can delete the team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to delete.</param>
    /// <param name="userId">The unique identifier of the user attempting to delete the team.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the team, conversation, and invites were successfully soft deleted,
    /// or an error message if the user does not have permission to delete the team.
    /// </returns>
    /// <remarks>
    /// This method performs a soft delete by setting DeletedAtUtc on the team, conversation, and pending invites.
    /// The operation only succeeds if the user is the team owner.
    /// </remarks>
    public async Task<Result<bool>> DeleteTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        bool canDelete = await teamRepository.GetTeamUserOwnsByIdAsync(teamId, userId, dbContext, ct) is not null;
        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this team.");
        }

        Team? team = await teamRepository.GetByIdAsync(teamId, userId, false, dbContext, ct);
        if (team is null)
        {
            return Result<bool>.Fail("Team not found.");
        }

        Conversation? conversation = team.Conversation;
        if (conversation is null)
        {
            return Result<bool>.Fail("Associated conversation not found.");
        }

        PaginationResponse<List<TeamInvite>> teamInvites = await teamInviteRepository.GetInvites(teamId, dbContext, 1, int.MaxValue, ct);

        teamRepository.Remove(team, dbContext);
        conversationRepository.Remove(conversation, dbContext);
        
        foreach (TeamInvite invite in teamInvites.Data)
        {
            teamInviteRepository.Remove(invite, dbContext);
        }

        int affected = await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(affected > 0);
    }

    /// <summary>
    /// Deletes the team's image from S3 storage and removes the image reference from the team. Only the team owner can delete the image.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team.</param>
    /// <param name="userId">The unique identifier of the user attempting to delete the image.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the image was successfully deleted, or an error message if:
    /// - Team not found or user is not the owner
    /// - Team does not have an image to delete
    /// - Failed to delete image from S3 storage
    /// </returns>
    public async Task<Result<bool>> DeleteTeamImageAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team? team = await teamRepository.GetTeamUserOwnsByIdAsync(teamId, userId, dbContext, ct);
        if (team is null)
        {
            return Result<bool>.Fail("Team not found or user is not the owner.");
        }

        if (team.Image is null || string.IsNullOrWhiteSpace(team.Image.Key))
        {
            return Result<bool>.Fail("Team does not have an image to delete.");
        }

        // Delete the image from S3
        Result<bool> deleteResult = await s3Service.DeleteFileAsync($"teams/{team.Id}/image/{team.Image.Key}");
        if (!deleteResult.Success)
        {
            return Result<bool>.Fail("Failed to delete image from storage.");
        }

        // Remove the image reference from the team
        team.Image = null;
        team.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    /// <summary>
    /// Removes a user from a team. If the user is the owner, the team is soft deleted.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to leave.</param>
    /// <param name="userId">The unique identifier of the user leaving the team.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing an integer indicating the operation result:
    /// - 1: Team was soft deleted (owner left)
    /// - 2: User was removed from the team (member left)
    /// Or an error message if:
    /// - Team not found or user is not a member
    /// - User is not a participant of the conversation
    /// - Failed to delete the team (if owner)
    /// - Failed to delete the associated conversation (if owner)
    /// - Associated conversation not found (if member)
    /// </returns>
    /// <remarks>
    /// If the owner leaves, the team and its conversation are soft deleted. If a regular member leaves,
    /// they are removed from the team and the associated conversation.
    /// </remarks>
    public async Task<Result<int>> LeaveTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team? team = await teamRepository.GetTeamUserParticipatesByIdAsync(teamId, userId, dbContext, ct);
        if (team is null)
        {
            return Result<int>.Fail("Team not found or user is not a member.");
        }

        ApplicationUser? me = team.Members.FirstOrDefault(p => p.Id == userId);
        if (me is null)
        {
            return Result<int>.Fail("User is not a participant of the conversation.");
        }

        team.Members.Remove(me);
        team.UpdatedAtUtc = DateTime.UtcNow;

        // If the user leaving is the owner, we soft delete the team
        bool mustSoftDelete = team.OwnerId == userId;
        if (mustSoftDelete)
        {
            Result<bool> deleteResult = await DeleteTeamAsync(team.Id, team.OwnerId, ct);
            if (!deleteResult.Success)
            {
                return Result<int>.Fail("Failed to delete the team: " + deleteResult.ErrorMessages[0]);
            }
        }
        else
        {
            Conversation? conversation =
                await conversationRepository.GetByIdAsync(team.ConversationId!, userId, dbContext, ct);
            if (conversation is not null)
            {
                conversation.Participants.Remove(me);
                conversation.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                return Result<int>.Fail("Associated conversation not found.");
            }
            await dbContext.SaveChangesAsync(ct);
        }

        return Result<int>.Ok(mustSoftDelete ? 1 : 2);
    }

    /// <summary>
    /// Adds a user to a team. For private teams or if the user has a pending invite, the invite must be accepted first.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to join.</param>
    /// <param name="userId">The unique identifier of the user joining the team.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the user successfully joined the team, or an error message if:
    /// - Failed to retrieve team invites
    /// - Team not found or user doesn't have access
    /// - User not found
    /// - User is already a member of the team
    /// - Failed to accept team invite (for private teams or users with invites)
    /// - Associated conversation not found
    /// </returns>
    /// <remarks>
    /// For public teams without invites, the user is added directly. For private teams or users with pending invites,
    /// the invite must be accepted first. The user is also added to the associated team conversation.
    /// A notification is sent to the team owner when a new member joins (unless the owner is joining).
    /// </remarks>
    public async Task<Result<bool>> JoinTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        Result<PaginationResponse<List<TeamInviteDto>>> results = await teamInvitesService.GetInvites(teamId, 1, int.MaxValue, ct);

        if (!results.Success)
        {
            return Result<bool>.Fail("Failed to retrieve team invites.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        TeamInviteDto? invite = results.Data.Data
            .Where(i => i is { Status: InviteStatus.Pending, IsExpired: false })
            .FirstOrDefault(i => i.ReceiverId == userId);

        Team? team = await teamRepository.GetByIdAsync(teamId, userId, invite != null, dbContext, ct);
        if (team is null)
        {
            return Result<bool>.Fail("Team not found or user doesn't have access.");
        }

        ApplicationUser? me = await userRepository.GetByIdAsync(userId, dbContext, ct);
        if (me is null)
        {
            return Result<bool>.Fail("User not found.");
        }

        // Check if user is already a member
        if (team.Members.Any(m => m.Id == userId))
        {
            return Result<bool>.Fail("User is already a member of this team.");
        }

        if (team.Privacy ==
            TeamPrivacy.Private) // if private, must accept invite, if have invite, must acept it first,  if public can join directly
        {
            if (invite == null)
            {
                return Result<bool>.Fail("Invite is required to join this private team.");
            }

            Result<TeamInviteDto> resultAcceptInvite = await teamInvitesService.AcceptInvite(invite.Id, userId, ct);
            if (!resultAcceptInvite.Success)
            {
                return Result<bool>.Fail("Failed to accept team invite: " + resultAcceptInvite.ErrorMessages[0]);
            }
        }
        else
        {
            team.Members.Add(me);
            team.UpdatedAtUtc = DateTime.UtcNow;
        }

        Conversation? conversation = team.Conversation;
        if (conversation is not null)
        {
            conversation.Participants.Add(me);
            conversation.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            return Result<bool>.Fail("Associated conversation not found.");
        }

        await dbContext.SaveChangesAsync(ct);

        // Notify team owner that someone joined the team
        if (team.OwnerId == userId)
        {
            return Result<bool>.Ok(true);
        }

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.Team,
            ReceiverUserId = team.OwnerId,
            SenderUserId = userId,
            RelatedEntityId = team.Id,
            RelatedEntityName = team.Name,
            Title = "New team member",
            Message = $"{me.DisplayName} joined the team {team.Name}",
            ActionUrl = $"/teams/{team.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);
        return Result<bool>.Ok(true);
    }
}