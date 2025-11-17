using Amazon.S3;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Services.Conversations;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Teams;

public class TeamService(
    ApplicationDbContext applicationDbContext,
    IS3Service s3Service,
    IConversationService conversationService,
    IOptions<S3Settings> s3Settings,
    IValidator<CreateTeamDto> createTeamValidator,
    IValidator<UpdateTeamDto> updateTeamValidator,
    UserManager<ApplicationUser> userManager) : ITeamService
{
    
    public async Task<Result<PaginationResponse<List<TeamDto>>>> GetTeamsAsync(
        string userId, int page, int pageSize, string q, CancellationToken ct = default)
    {
        List<string> invitedTeamIds = await applicationDbContext
            .TeamInvites
            .Where(i => i.ReceiverId == userId)
            .Select(i => i.TeamId)
            .ToListAsync(ct);
        
        IQueryable<Team> query = applicationDbContext
            .Teams
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Conversation)
                .ThenInclude(c => c!.Participants)
            .Include(t => t.Conversation)
                .ThenInclude(c => c!.Messages)
            .Where(m => m.Privacy == TeamPrivacy.Public || invitedTeamIds.Contains(m.Id))
            .AsNoTracking()
            .AsSplitQuery();
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c => c.Name.ToLower().Contains(q.ToLower()) || c.Description.ToLower().Contains(q.ToLower()));
        }

        int total = await query.CountAsync(ct);

        List<Team> teams = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        foreach (ApplicationUser u in teams.SelectMany(u => u.Members))
        {
            await RefreshProfileImage(u);
        }

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
        bool hasInvite = await applicationDbContext
            .TeamInvites
            .AnyAsync(i => i.TeamId == teamId && i.ReceiverId == userId, ct);
        
        Team? team = await applicationDbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .ThenInclude(r => r!.Participants)
            .Include(m => m.Members)
            .Where(m => m.Id.Equals(teamId))
            .Where(m => m.Privacy == TeamPrivacy.Public || hasInvite || m.Members.Any(u => u.Id.Equals(userId)))
            .FirstOrDefaultAsync(ct);

        if (team is null)
        {
            return Result<TeamDto>.Fail("Team not found or access denied.");
        }
        
        return Result<TeamDto>.Ok(team.ToDto());
    }


    public async Task<Result<TeamDto>> CreateTeamAsync(CreateTeamDto createTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await createTeamValidator.ValidateAsync(createTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        Team team = createTeamDto.ToEntity();
        
        ApplicationUser? owner = await userManager.Users.FirstOrDefaultAsync(p => p.Id == createTeamDto.OwnerId, cancellationToken: ct);
        if (owner is null)
        {
            return Result<TeamDto>.Fail("User not found.");
        }
        
        // Don't add members directly - they will be added via invites
        team.Members = (List<ApplicationUser>) [owner];
        team.ConversationId = null; // will be set after conversation is created, to not give errors on FK constraint

        await applicationDbContext.Teams.AddAsync(team, ct);
        await applicationDbContext.SaveChangesAsync(ct);
        
        var conversationCreationDto = new DTOs.Chat.Conversations.CreateConversationDto
        {
            CreatorUserId = team.OwnerId,
            ConversationType = ConversationType.Team,
            Title = team.Name,
            ParticipantIds = [createTeamDto.OwnerId],
            TeamId = team.Id
        };
        
        Result<DTOs.Chat.Conversations.ConversationDto> conversationResult =
            await conversationService.CreateConversationAsync(conversationCreationDto, ct);
        
        if (!conversationResult.Success)
        {
            return Result<TeamDto>.Fail("Failed to create associated conversation: " + conversationResult.ErrorMessages[0]);
        }
        
        team.ConversationId = conversationResult.Data!.Id; // since we set the conversation's ID to be the same as the team's ID
        await applicationDbContext.SaveChangesAsync(ct);
        
        // Send invites to selected users
        if (createTeamDto.MembersIds.All(m => m == createTeamDto.OwnerId))
        {
            return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
        }

        foreach (string receiverId in createTeamDto.MembersIds)
        {
            await SendTeamInviteAsync(team.Id, createTeamDto.OwnerId, receiverId, $"You've been invited to join {team.Name}!", ct);
        }

        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    public async Task<Result<TeamDto>> UpdateTeamAsync(UpdateTeamDto updateTeamDto, CancellationToken ct = default)
    {
        ValidationResult? validationResult = await updateTeamValidator.ValidateAsync(updateTeamDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamDto>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        // only the creator can update participants
        Team? team = await applicationDbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .Include(m => m.Members)
            .Where(m => m.Id.Equals(updateTeamDto.Id))
            .Where(m => m.OwnerId.Equals(updateTeamDto.OwnerId))
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
        
        Conversation? conversation = await applicationDbContext
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
        
        await applicationDbContext.SaveChangesAsync(ct);
        return await GetTeamByIdAsync(team.Id, team.OwnerId, ct);
    }

    public async Task<Result<bool>> DeleteTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        bool canDelete = await applicationDbContext.Teams
            .AnyAsync(c => c.Id == teamId && c.OwnerId.Equals(userId, StringComparison.InvariantCulture), ct);

        if (!canDelete)
        {
            return Result<bool>.Fail("User does not have permission to delete this team.");
        }

        int affectedTeams = await applicationDbContext
            .Teams
            .Where(c => c.Id == teamId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);
        
        int affectedConversations = await applicationDbContext
            .Conversations
            .Where(c => c.Id == teamId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.DeletedAtUtc, DateTime.UtcNow), ct);

        return Result<bool>.Ok(affectedTeams > 0 && affectedConversations > 0);
    }

    public async Task<Result<int>> LeaveTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        Team? team = await applicationDbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .Include(m => m.Members)
            .Where(m => m.Id.Equals(teamId))
            .Where(m => m.Members.Any(u => u.Id.Equals(userId)))
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
        }
        
        Conversation? conversation = await applicationDbContext
            .Conversations
            .Where(c => c.Id == team.ConversationId)
            .FirstOrDefaultAsync(ct);
        
        if (conversation is not null)
        {
            conversation.Participants.Remove(me);
            conversation.UpdatedAtUtc = DateTime.UtcNow;

            if (mustSoftDelete)
            {
                conversation.DeletedAtUtc = DateTime.UtcNow;
            }
        }
        else
        {
            return Result<int>.Fail("Associated conversation not found.");
        }

        await applicationDbContext.SaveChangesAsync(ct);

        return Result<int>.Ok(mustSoftDelete ? 1 : 2);
    }
    
    public async Task<Result<bool>> JoinTeamAsync(string teamId, string userId, CancellationToken ct = default)
    {
        bool hasInvite = await applicationDbContext
            .TeamInvites
            .AnyAsync(i => i.TeamId == teamId && i.ReceiverId == userId, ct);
        
        Team? team = await applicationDbContext
            .Teams
            .Include(m => m.Owner)
            .Include(m => m.Conversation)
            .Include(m => m.Members)
            .Where(m => m.Id.Equals(teamId))
            .Where(m => m.Privacy == TeamPrivacy.Public || hasInvite)
            .FirstOrDefaultAsync(ct);

        if (team is null)
        {
            return Result<bool>.Fail("Team not found or user doesn't have access.");
        }

        ApplicationUser? me = await userManager.Users.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken: ct);
        if (me is null)
        {
            return Result<bool>.Fail("User not found.");
        }

        team.Members.Add(me);
        team.UpdatedAtUtc = DateTime.UtcNow;
        
        Conversation? conversation = await applicationDbContext
            .Conversations
            .Where(c => c.Id == team.ConversationId)
            .FirstOrDefaultAsync(ct);
        
        if (conversation is not null)
        {
            conversation.Participants.Add(me);
            conversation.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            return Result<bool>.Fail("Associated conversation not found.");
        }
        
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> SendTeamInviteAsync(string teamId, string senderId, string receiverId, string content, CancellationToken ct = default)
    {
        // Check if team exists and sender is owner
        Team? team = await applicationDbContext.Teams
            .FirstOrDefaultAsync(t => t.Id == teamId && t.OwnerId == senderId, ct);
        
        if (team is null)
        {
            return Result<bool>.Fail("Team not found or user is not the owner.");
        }

        // Check if receiver is already a member
        bool isMember = await applicationDbContext.Teams
            .Where(t => t.Id == teamId)
            .AnyAsync(t => t.Members.Any(m => m.Id == receiverId), ct);
        
        if (isMember)
        {
            return Result<bool>.Fail("User is already a member of this team.");
        }

        // Check if invite already exists
        bool inviteExists = await applicationDbContext.TeamInvites
            .AnyAsync(i => i.TeamId == teamId && i.ReceiverId == receiverId && i.DeletedAtUtc == null, ct);
        
        if (inviteExists)
        {
            return Result<bool>.Fail("Invite already sent to this user.");
        }

        //remove this from here
        var invite = new TeamInvite
        {
            Id = $"teamInvite_{Guid.CreateVersion7()}",
            TeamId = teamId,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            CreatedAtUtc = DateTime.UtcNow
        };

        await applicationDbContext.TeamInvites.AddAsync(invite, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteTeamInviteAsync(string inviteId, string userId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext.TeamInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && (i.SenderId == userId || i.ReceiverId == userId), ct);
        
        if (invite is null)
        {
            return Result<bool>.Fail("Invite not found or user doesn't have permission.");
        }

        invite.DeletedAtUtc = DateTime.UtcNow;
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private async Task RefreshProfileImage(ApplicationUser user)
    {
        if (user.ProfileImage?.Key is null)
        {
            return;
        }

        if (user.ProfileImage.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(user.ProfileImage.Url))
        {
            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}", HttpVerb.GET);

            if (url.Success)
            {
                user.ProfileImage = user.ProfileImage with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }
    }
}
