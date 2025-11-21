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
using MatchBy.Services.Conversations;
using MatchBy.Services.ImageRefresh;
using MatchBy.Services.Notifications;
using MatchBy.Services.S3;
using MatchBy.Services.TeamInvites;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Teams;

public class TeamService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IS3Service s3Service,
    IConversationService conversationService,
    ITeamsInvitesService teamInvitesService,
    IValidator<CreateTeamDto> createTeamValidator,
    IValidator<UpdateTeamDto> updateTeamValidator,
    IImageRefreshService imageRefreshService,
    INotificationService notificationService) : ITeamService
{
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        List<string> invitedTeamIds = await dbContext
            .TeamInvites
            .Where(i => i.ReceiverId == teamQueryParametersDto.UserId)
            .Where(i => i.Status == InviteStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow)
            .Select(i => i.TeamId)
            .ToListAsync(ct);

        IQueryable<Team> query = dbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages)
            .AsNoTracking();

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

        if (!string.IsNullOrWhiteSpace(teamQueryParametersDto.Query))
        {
            query = query.Where(c =>
                c.Name.ToLower().Contains(teamQueryParametersDto.Query.ToLower()) ||
                c.Description.ToLower().Contains(teamQueryParametersDto.Query.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Team> teams = await query
            .Skip((teamQueryParametersDto.Page - 1) * teamQueryParametersDto.PageSize)
            .Take(teamQueryParametersDto.PageSize)
            .ToListAsync(ct);

        var distinctUsers = teams.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctUsers.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = teams.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        var list = teams.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(
            new PaginationResponse<List<TeamDto>>
            {
                Data = list,
                TotalCount = total,
                Page = teamQueryParametersDto.Page,
                PageSize = teamQueryParametersDto.PageSize
            });
    }

    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetAvailableTeamsAsync(
        TeamQueryParametersDto teamQueryParametersDto, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        List<string> invitedTeamIds = await dbContext
            .TeamInvites
            .Where(i => i.ReceiverId == teamQueryParametersDto.UserId)
            .Where(i => i.Status == InviteStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow)
            .Select(i => i.TeamId)
            .ToListAsync(ct);

        IQueryable<Team> query = dbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages)
            .Where(t => t.Members.All(u => u.Id != teamQueryParametersDto.UserId) && t.OwnerId != teamQueryParametersDto.UserId)
            .AsNoTracking();

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

        if (!string.IsNullOrWhiteSpace(teamQueryParametersDto.Query))
        {
            query = query.Where(c =>
                c.Name.ToLower().Contains(teamQueryParametersDto.Query.ToLower()) ||
                c.Description.ToLower().Contains(teamQueryParametersDto.Query.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Team> teams = await query
            .Skip((teamQueryParametersDto.Page - 1) * teamQueryParametersDto.PageSize)
            .Take(teamQueryParametersDto.PageSize)
            .ToListAsync(ct);

        var distinctTeams = teams.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctTeams.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = teams.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        var list = teams.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(
            new PaginationResponse<List<TeamDto>>
            {
                Data = list,
                TotalCount = total,
                Page = teamQueryParametersDto.Page,
                PageSize = teamQueryParametersDto.PageSize
            });
    }
    
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserOwnAsync(
        string userId, int page, int pageSize, string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<PaginationResponse<List<TeamDto>>>.Fail("User ID cannot be null or empty.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        IQueryable<Team> query = dbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages)
            .Where(m => m.OwnerId == userId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.Name.ToLower().Contains(q.ToLower()) || c.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Team> teams = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Refresh images in parallel for better performance
        var distinctUsers = teams.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctUsers.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = teams.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        var list = teams.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(
            new PaginationResponse<List<TeamDto>>
            {
                Data = list,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
    }

    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsUserParticipateAsync(
        string userId, int page, int pageSize, string q, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Team> query = dbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages)
            .Where(m => m.Members.Any(u => u.Id == userId))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c =>
                c.Name.ToLower().Contains(q.ToLower()) || c.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Team> teams = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Refresh images in parallel for better performance
        var distinctUsers = teams.SelectMany(t => t.Members).DistinctBy(u => u.Id).ToList();
        IEnumerable<Task> userImageTasks = distinctUsers.Select(imageRefreshService.RefreshUserProfileImageAsync);
        IEnumerable<Task> teamImageTasks = teams.Select(imageRefreshService.RefreshTeamImagesAsync);
        await Task.WhenAll(userImageTasks.Concat(teamImageTasks));

        var list = teams.Select(team => team.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamDto>>>.Ok(
            new PaginationResponse<List<TeamDto>>
            {
                Data = list,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
    }

    public async Task<Result<TeamDto>> GetTeamByIdAsync(string teamId, string userId, CancellationToken ct = default)
    {
        Result<PaginationResponse<List<TeamInviteDto>>> invitesResult =
            await teamInvitesService.GetInvitesForTeam(teamId, 1, int.MaxValue, ct);

        if (!invitesResult.Success)
        {
            return Result<TeamDto>.Fail("Failed to retrieve team invites.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        bool hasInvite = invitesResult.Data!.Data.Where(i => i is { Status: InviteStatus.Pending, IsExpired: false })
            .Any(i => i.ReceiverId == userId);

        Team? team = await dbContext
            .Teams
            .Include(m => m.Owner)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
            .ThenInclude(c => c!.Messages)
            .Include(m => m.Members)
            .Where(m => m.Id == teamId)
            .Where(m => m.Privacy == TeamPrivacy.Public || hasInvite || m.Members.Any(u => u.Id == userId))
            .FirstOrDefaultAsync(ct);

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

    public async Task<Result<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createTeamValidator.ValidateAsync(createTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team team = createTeamDto.ToEntity();

        ApplicationUser? owner =
            await dbContext.Users.FirstOrDefaultAsync(p => p.Id == createTeamDto.OwnerId, cancellationToken: ct);
        if (owner is null)
        {
            return Result<TeamDto>.Fail("User not found.");
        }

        // Don't add members directly - they will be added via invites
        team.Members = new List<ApplicationUser> { owner };
        team.ConversationId = null; // will be set after conversation is created, to not give errors on FK constraint

        await dbContext.Teams.AddAsync(team, ct);
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
            conversationResult.Data!.Id; // since we set the conversation's ID to be the same as the team's ID

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
            return await UpdateTeamImageAsync(team, createTeamDto.OwnerId, createTeamDto.File, dbContext, ct);
        }

        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    private async Task<Result<TeamDto>> UpdateTeamImageAsync(
        Team team,
        string userId,
        IBrowserFile file,
        ApplicationDbContext dbContext,
        CancellationToken ct = default)
    {
        // upload
        Result<string> uploadedKey = await s3Service.UploadBrowserFileAsync(file, $"teams/{team.Id}/image");
        if (!uploadedKey.Success)
        {
            return Result<TeamDto>.Fail(uploadedKey.ErrorMessages.ToArray());
        }

        // URL presign
        Result<string> url =
            await s3Service.GetPresignedUrlAsync($"teams/{team.Id}/image/{uploadedKey.Data}", HttpVerb.GET);
        if (!url.Success)
        {
            return Result<TeamDto>.Fail(url.ErrorMessages.ToArray());
        }

        // delete previous, if it exists
        string? oldKey = team.Image?.Key;
        if (!string.IsNullOrWhiteSpace(oldKey) && !oldKey.Equals(uploadedKey.Data, StringComparison.OrdinalIgnoreCase))
        {
            await s3Service.DeleteFileAsync($"teams/{team.Id}/image/{oldKey}");
        }

        // store the image info
        team.Image = new FileStore(
            Url: url.Data!,
            ExpireDateTimeUtc: DateTime.UtcNow.AddMinutes(30),
            Key: uploadedKey.Data!,
            FileCategory: FileCategory.TeamImage,
            FileType: FileType.Image,
            CreatedAtUtc: DateTime.UtcNow
        );
        team.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetTeamByIdAsync(team.Id, userId, ct);
    }

    public async Task<Result<TeamDto>> UpdateTeamAsync(UpdateTeamDto updateTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateTeamValidator.ValidateAsync(updateTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // only the creator can update participants
        Team? team = await dbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .Include(m => m.Members)
            .Where(m => m.Id == updateTeamDto.Id)
            .Where(m => m.OwnerId == updateTeamDto.OwnerId)
            .FirstOrDefaultAsync(ct);
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

        Conversation? conversation = await dbContext
            .Conversations
            .Where(c => c.Id == team.ConversationId)
            .FirstOrDefaultAsync(ct);

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
            return await UpdateTeamImageAsync(team, updateTeamDto.OwnerId, updateTeamDto.File, dbContext, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    public async Task<Result<bool>> DeleteTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        bool canDelete = await dbContext.Teams
            .AnyAsync(c => c.Id == teamId && c.OwnerId == userId, ct);

        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this team.");
        }

        int affectedTeams = await dbContext
            .Teams
            .Where(c => c.Id == teamId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        int affectedConversations = await dbContext
            .Conversations
            .Where(c => c.Id == teamId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        int affectedInvites = await dbContext
            .TeamInvites
            .Where(i => i.TeamId == teamId && i.Status == InviteStatus.Pending)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Status, InviteStatus.Cancelled)
                .SetProperty(i => i.DeletedAtUtc, DateTime.UtcNow), ct);

        return Result<bool>.Ok(affectedTeams > 0 && affectedConversations > 0 && affectedInvites >= 0);
    }

    public async Task<Result<bool>> DeleteTeamImageAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team? team = await dbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Members)
            .Where(m => m.Id == teamId)
            .Where(m => m.OwnerId == userId)
            .FirstOrDefaultAsync(ct);

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

    public async Task<Result<int>> LeaveTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Team? team = await dbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .Include(m => m.Members)
            .Where(m => m.Id == teamId)
            .Where(m => m.Members.Any(u => u.Id == userId))
            .FirstOrDefaultAsync(ct);

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
            team.DeletedAtUtc = DateTime.UtcNow;
            Result<bool> deleteResult = await DeleteTeamAsync(team.Id, team.OwnerId, ct);
            if (!deleteResult.Success)
            {
                return Result<int>.Fail("Failed to delete the team: " + deleteResult.ErrorMessages[0]);
            }

            Result<bool> deleteConversation =
                await conversationService.DeleteConversationAsync(team.ConversationId!, team.OwnerId, ct);
            if (!deleteConversation.Success)
            {
                return Result<int>.Fail("Failed to delete the associated conversation: " +
                                        deleteConversation.ErrorMessages[0]);
            }
        }
        else
        {
            Conversation? conversation = await dbContext
                .Conversations
                .Include(c => c.Participants)
                .Where(c => c.Id == team.ConversationId)
                .FirstOrDefaultAsync(ct);

            if (conversation is not null)
            {
                conversation.Participants.Remove(me);
                conversation.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                return Result<int>.Fail("Associated conversation not found.");
            }
        }

        await dbContext.SaveChangesAsync(ct);

        return Result<int>.Ok(mustSoftDelete ? 1 : 2);
    }

    public async Task<Result<bool>> JoinTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        Result<PaginationResponse<List<TeamInviteDto>>> results =
            await teamInvitesService.GetInvitesForTeam(teamId, 1, int.MaxValue, ct);

        if (!results.Success)
        {
            return Result<bool>.Fail("Failed to retrieve team invites.");
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        TeamInviteDto? invite = results.Data!.Data
            .Where(i => i is { Status: InviteStatus.Pending, IsExpired: false })
            .FirstOrDefault(i => i.ReceiverId == userId);

        Team? team = await dbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .ThenInclude(c => c!.Participants)
            .Include(m => m.Members)
            .Where(m => m.Id == teamId)
            .Where(m => m.Privacy == TeamPrivacy.Public || invite != null)
            .FirstOrDefaultAsync(ct);

        if (team is null)
        {
            return Result<bool>.Fail("Team not found or user doesn't have access.");
        }

        ApplicationUser? me = await dbContext.Users.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken: ct);
        if (me is null)
        {
            return Result<bool>.Fail("User not found.");
        }

        // Check if user is already a member
        if (team.Members.Any(m => m.Id == userId))
        {
            return Result<bool>.Fail("User is already a member of this team.");
        }

        if (team.Privacy == TeamPrivacy.Private || invite != null) // if private, must accept invite, if have invite, must acept it first,  if public can join directly
        {
            Result<TeamInviteDto> resultAcceptInvite = await teamInvitesService.AcceptInvite(
                invite?.Id ?? "NoInvite", userId, ct);

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

        string? newMember = me.DisplayName;

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.TeamMemberJoined,
            ReceiverUserId = team.OwnerId,
            SenderUserId = userId,
            RelatedEntityId = team.Id,
            RelatedEntityName = team.Name,
            Title = "New team member",
            Message = $"{newMember} joined the team {team.Name}",
            ActionUrl = $"/teams/{team.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return Result<bool>.Ok(true);
    }
}