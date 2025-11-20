using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.TeamInvites;

public class TeamsInvitesService(
    ApplicationDbContext applicationDbContext,
    IValidator<CreateTeamInviteDto> createInviteValidator,
    IValidator<UpdateTeamInviteDto> updateInviteValidator) : ITeamsInvitesService
{
    public async Task<Result<TeamInviteDto>> GetInviteById(string inviteId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext
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

    public async Task<Result<PaginationResponse<List<TeamInviteDto>>>> GetReceivedInvites(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<TeamInvite> query = applicationDbContext
            .TeamInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Owner)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Members)
            .Where(i => i.ReceiverId == userId);

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

    public async Task<Result<PaginationResponse<List<TeamInviteDto>>>> GetSentInvites(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        IQueryable<TeamInvite> query = applicationDbContext
            .TeamInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Owner)
            .Include(i => i.Team)
                .ThenInclude(t => t!.Members)
            .Where(i => i.SenderId == userId);

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

    public async Task<Result<PaginationResponse<List<TeamInviteDto>>>> GetInvitesForTeam(
        string teamId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        // First, check if the team exists
        bool teamExists = await applicationDbContext.Teams.AnyAsync(t => t.Id == teamId, ct);
        if (!teamExists)
        {
            return Result<PaginationResponse<List<TeamInviteDto>>>.Fail($"Team with id {teamId} not found.");
        }

        IQueryable<TeamInvite> query = applicationDbContext
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

    public async Task<Result<TeamInviteDto>> CreateInvite(CreateTeamInviteDto createDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await createInviteValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamInviteDto>.Fail(validationResult.ToString());
        }

        // Check if sender exists
        bool senderExists = await applicationDbContext.Users.AnyAsync(u => u.Id == createDto.SenderId, ct);
        if (!senderExists)
        {
            return Result<TeamInviteDto>.Fail($"Sender with id {createDto.SenderId} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await applicationDbContext.Users.AnyAsync(u => u.Id == createDto.ReceiverId, ct);
        if (!receiverExists)
        {
            return Result<TeamInviteDto>.Fail($"Receiver with id {createDto.ReceiverId} not found.");
        }

        // Check if team exists
        Team? team = await applicationDbContext.Teams
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
        bool existingInvite = await applicationDbContext.TeamInvites
            .AnyAsync(i => i.TeamId == createDto.TeamId 
                        && i.ReceiverId == createDto.ReceiverId 
                        && i.Status == InviteStatus.Pending && i.ExpiresAtUtc > DateTime.UtcNow, ct);
        
        if (existingInvite)
        {
            return Result<TeamInviteDto>.Fail($"A pending invite already exists for this user and team.");
        }

        TeamInvite invite = createDto.ToEntity();
        await applicationDbContext.TeamInvites.AddAsync(invite, ct);
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<TeamInviteDto>> UpdateInvite(UpdateTeamInviteDto updateDto, string userId, CancellationToken ct = default)
    {
        ValidationResult validationResult = await updateInviteValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<TeamInviteDto>.Fail(validationResult.ToString());
        }

        TeamInvite? invite = await applicationDbContext.TeamInvites
            .FirstOrDefaultAsync(i => i.Id == updateDto.Id, ct);

        if (invite == null)
        {
            return Result<TeamInviteDto>.Fail($"Invite with id {updateDto.Id} not found.");
        }

        // Only sender can update the invite
        if (invite.SenderId != userId)
        {
            return Result<TeamInviteDto>.Fail("Only the sender can update the invite.");
        }

        invite.UpdateEntity(updateDto);
        await applicationDbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<bool>> DeleteInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext.TeamInvites
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

        invite.DeletedAtUtc = DateTime.UtcNow;
        await applicationDbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<TeamInviteDto>> AcceptInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext.TeamInvites
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
            await applicationDbContext.SaveChangesAsync(ct);
            return Result<TeamInviteDto>.Fail("The invite has expired.");
        }

        // Check if team still has space
        if (invite.Team!.Members.Count >= invite.Team.MaxMembers)
        {
            return Result<TeamInviteDto>.Fail("The team is already full.");
        }

        // Add user to team members
        ApplicationUser? user = await applicationDbContext.Users.FindAsync([userId], ct);
        if (user == null)
        {
            return Result<TeamInviteDto>.Fail($"User with id {userId} not found.");
        }

        invite.Team.Members.Add(user);
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAtUtc = DateTime.UtcNow;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<TeamInviteDto>> DeclineInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext.TeamInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<TeamInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only receiver can decline the invite
        if (invite.ReceiverId != userId)
        {
            return Result<TeamInviteDto>.Fail("Only the receiver can decline the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<TeamInviteDto>.Fail($"Cannot decline an invite with status {invite.Status}.");
        }

        invite.Status = InviteStatus.Declined;
        invite.DeclinedAtUtc = DateTime.UtcNow;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<TeamInviteDto>> CancelInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        TeamInvite? invite = await applicationDbContext.TeamInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<TeamInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only sender can cancel the invite
        if (invite.SenderId != userId)
        {
            return Result<TeamInviteDto>.Fail("Only the sender can cancel the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<TeamInviteDto>.Fail($"Cannot cancel an invite with status {invite.Status}.");
        }

        invite.Status = InviteStatus.Cancelled;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await applicationDbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }
}
