using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Notification;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.TeamInvites;

public class TeamsInvitesService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateTeamInviteDto> createInviteValidator,
    INotificationService notificationService) : ITeamsInvitesService
{
    /// <summary>
    /// Retrieves a paginated list of team invites for a specific team.
    /// </summary>
    /// <param name="teamId">The unique identifier of the team to get invites for.</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of invites per page (default: 10).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with a list of team invite DTOs if successful,
    /// or an error message if the team is not found.
    /// </returns>
    public async Task<Result<PaginationResponse<List<TeamInviteDto>>>> GetInvites(
        string teamId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // First, check if the team exists
        bool teamExists = await dbContext.Teams.AnyAsync(t => t.Id == teamId, ct);
        if (!teamExists)
        {
            return Result<PaginationResponse<List<TeamInviteDto>>>.Fail($"Team with id {teamId} not found.");
        }

        IQueryable<TeamInvite> query = dbContext
            .TeamInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Owner)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Members)
            .Where(i => i.TeamId == teamId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<TeamInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var inviteDtos = invites.Select(i => i.ToDto()).ToList();

        return Result<PaginationResponse<List<TeamInviteDto>>>.Ok(
            new PaginationResponse<List<TeamInviteDto>>
            {
                Data = inviteDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }
    /// <summary>
    /// Retrieves a specific team invite by its unique identifier.
    /// </summary>
    /// <param name="inviteId">The unique identifier of the invite to retrieve.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the team invite DTO if found, or an error message if the invite does not exist.
    /// </returns>
    public async Task<Result<TeamInviteDto>> GetInviteById(string inviteId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        TeamInvite? invite = await dbContext
            .TeamInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Team)
            .ThenInclude(t => t!.Owner)
            .Include(i => i.Team)
            .ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        return invite == null
            ? Result<TeamInviteDto>.Fail($"Invite with id {inviteId} not found.")
            : Result<TeamInviteDto>.Ok(invite.ToDto());
    }
    /// <summary>
    /// Creates a new team invite and sends a notification to the receiver.
    /// </summary>
    /// <param name="createDto">The DTO containing the invite creation details (sender, receiver, team, etc.).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the created team invite DTO if successful, or an error message if:
    /// - Validation fails
    /// - Sender, receiver, or team does not exist
    /// - Receiver is already a team member
    /// - Team is already full
    /// - A pending invite already exists for this user and team
    /// </returns>
    /// <remarks>
    /// This method performs validation, checks business rules, creates the invite,
    /// and automatically sends a notification to the receiver.
    /// </remarks>
    public async Task<Result<TeamInviteDto>> CreateInvite(CreateTeamInviteDto createDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await createInviteValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamInviteDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if sender exists
        bool senderExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.SenderId, ct);
        if (!senderExists)
        {
            return Result<TeamInviteDto>.Fail($"Sender with id {createDto.SenderId} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.ReceiverId, ct);
        if (!receiverExists)
        {
            return Result<TeamInviteDto>.Fail($"Receiver with id {createDto.ReceiverId} not found.");
        }

        // Check if team exists
        Team? team = await dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == createDto.TeamId, ct);
        
        if (team == null)
        {
            return Result<TeamInviteDto>.Fail($"Team with id {createDto.TeamId} not found.");
        }

        // Check if receiver is already a member
        if (team.Members.Any(m => m.Id == createDto.ReceiverId))
        {
            return Result<TeamInviteDto>.Fail($"User {createDto.ReceiverId} is already a member of this team.");
        }

        // Check if team is full
        if (team.Members.Count >= team.MaxMembers)
        {
            return Result<TeamInviteDto>.Fail("The team is already full.");
        }

        // Check if there's already a pending invite
        bool existingInvite = await dbContext.TeamInvites
            .AnyAsync(i => i.TeamId == createDto.TeamId 
                        && i.ReceiverId == createDto.ReceiverId 
                        && i.Status == InviteStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow, ct);
        
        if (existingInvite)
        {
            return Result<TeamInviteDto>.Fail($"A pending invite already exists for this user and team.");
        }

        TeamInvite invite = createDto.ToEntity();
        await dbContext.TeamInvites.AddAsync(invite, ct);
        await dbContext.SaveChangesAsync(ct);

        // Send notification to the receiver
        string? sender = await dbContext.Users
            .Where(u => u.Id == createDto.SenderId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct);
        
        var notification = new CreateNotificationDto
        {
            Type = NotificationType.TeamInviteReceived,
            ReceiverUserId = createDto.ReceiverId,
            SenderUserId = createDto.SenderId,
            RelatedEntityId = team.Id,
            RelatedEntityName = team.Name,
            Title = "Team invite",
            Message = $"{sender} invited you to the team {team.Name}",
            ActionUrl = $"/teams/{team.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return await GetInviteById(invite.Id, ct);
    }
    
    /// <summary>
    /// Soft deletes a team invite by marking it as deleted. Only the sender can delete their own invite.
    /// </summary>
    /// <param name="inviteId">The unique identifier of the invite to delete.</param>
    /// <param name="userId">The unique identifier of the user attempting to delete the invite.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing true if the invite was successfully deleted, or an error message if:
    /// - The invite does not exist
    /// - The user is not the sender of the invite
    /// </returns>
    public async Task<Result<bool>> DeleteInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        TeamInvite? invite = await dbContext.TeamInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<bool>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only sender can delete the invite
        if (invite.SenderId != userId)
        {
            return Result<bool>.Fail("Only the sender can delete the invite.");
        }

        invite.Status = InviteStatus.Deleted;
        invite.DeletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
    /// <summary>
    /// Accepts a team invite and adds the user to the team. Only the receiver can accept their own invite.
    /// </summary>
    /// <param name="inviteId">The unique identifier of the invite to accept.</param>
    /// <param name="userId">The unique identifier of the user attempting to accept the invite.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing the updated team invite DTO if successful, or an error message if:
    /// - The invite does not exist
    /// - The user is not the receiver of the invite
    /// - The invite status is not Pending
    /// - The invite has expired
    /// - The team is already full
    /// - The user does not exist
    /// </returns>
    /// <remarks>
    /// When an invite is accepted, the user is added to the team's members list,
    /// and the invite status is updated to Accepted.
    /// </remarks>
    public async Task<Result<TeamInviteDto>> AcceptInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        TeamInvite? invite = await dbContext.TeamInvites
            .Include(i => i.Team)
                .ThenInclude(t => t!.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<TeamInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only receiver can accept the invite
        if (invite.ReceiverId != userId)
        {
            return Result<TeamInviteDto>.Fail("Only the receiver can accept the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<TeamInviteDto>.Fail($"Cannot accept an invite with status {invite.Status}.");
        }

        if (invite.IsExpired)
        {
            invite.Status = InviteStatus.Expired;
            await dbContext.SaveChangesAsync(ct);
            return Result<TeamInviteDto>.Fail("The invite has expired.");
        }

        // Check if team still has space
        if (invite.Team!.Members.Count >= invite.Team.MaxMembers)
        {
            return Result<TeamInviteDto>.Fail("The team is already full.");
        }

        // Add user to team members
        ApplicationUser? user = await dbContext.Users.FindAsync([userId], ct);
        if (user == null)
        {
            return Result<TeamInviteDto>.Fail($"User with id {userId} not found.");
        }

        invite.Team.Members.Add(user);
        invite.Team.UpdatedAtUtc = DateTime.UtcNow;
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAtUtc = DateTime.UtcNow;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }
}
