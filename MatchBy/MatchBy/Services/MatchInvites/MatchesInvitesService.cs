using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.Notification;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.MatchInvites;

public class MatchesInvitesService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateMatchInviteDto> createInviteValidator,
    IValidator<UpdateMatchInviteDto> updateInviteValidator,
    INotificationService notificationService) : IMatchesInvitesService
{
    public async Task<Result<MatchInviteDto>> GetInviteById(string inviteId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext
            .MatchInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Creator)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Participants)
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        return invite == null
            ? Result<MatchInviteDto>.Fail($"Invite with id {inviteId} not found.")
            : Result<MatchInviteDto>.Ok(invite.ToDto());
    }

    public async Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetReceivedInvites(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<MatchInvite> query = dbContext
            .MatchInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Creator)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Participants)
            .Where(i => i.ReceiverId == userId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var inviteDtos = invites.Select(i => i.ToDto()).ToList();

        return Result<PaginationResponse<List<MatchInviteDto>>>.Ok(
            new PaginationResponse<List<MatchInviteDto>>
            {
                Data = inviteDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetSentInvites(
        string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<MatchInvite> query = dbContext
            .MatchInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Creator)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Participants)
            .Where(i => i.SenderId == userId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var inviteDtos = invites.Select(i => i.ToDto()).ToList();

        return Result<PaginationResponse<List<MatchInviteDto>>>.Ok(
            new PaginationResponse<List<MatchInviteDto>>
            {
                Data = inviteDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<MatchInviteDto>>>> GetInvitesForMatch(
        string matchId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // First, check if the match exists
        bool matchExists = await dbContext.Matches.AnyAsync(m => m.Id == matchId, ct);
        if (!matchExists)
        {
            return Result<PaginationResponse<List<MatchInviteDto>>>.Fail($"Match with id {matchId} not found.");
        }

        IQueryable<MatchInvite> query = dbContext
            .MatchInvites
            .AsNoTracking()
            .Include(i => i.Sender)
            .Include(i => i.Receiver)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Creator)
            .Include(i => i.Match)
                .ThenInclude(m => m!.Participants)
            .Where(i => i.MatchId == matchId);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<MatchInvite> invites = await query
            .OrderByDescending(i => i.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var inviteDtos = invites.Select(i => i.ToDto()).ToList();

        return Result<PaginationResponse<List<MatchInviteDto>>>.Ok(
            new PaginationResponse<List<MatchInviteDto>>
            {
                Data = inviteDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<MatchInviteDto>> CreateInvite(CreateMatchInviteDto createDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await createInviteValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<MatchInviteDto>.Fail(validationResult.ToString());
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Check if sender exists
        bool senderExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.SenderId, ct);
        if (!senderExists)
        {
            return Result<MatchInviteDto>.Fail($"Sender with id {createDto.SenderId} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.ReceiverId, ct);
        if (!receiverExists)
        {
            return Result<MatchInviteDto>.Fail($"Receiver with id {createDto.ReceiverId} not found.");
        }

        // Check if match exists
        Match? match = await dbContext.Matches
            .Include(m => m.Participants)
            .FirstOrDefaultAsync(m => m.Id == createDto.MatchId, ct);
        
        if (match == null)
        {
            return Result<MatchInviteDto>.Fail($"Match with id {createDto.MatchId} not found.");
        }

        // Check if receiver is already a participant
        if (match.Participants.Any(p => p.Id == createDto.ReceiverId))
        {
            return Result<MatchInviteDto>.Fail($"User {createDto.ReceiverId} is already a participant in this match.");
        }

        // Check if there's already a pending invite
        bool existingInvite = await dbContext.MatchInvites
            .AnyAsync(i => i.MatchId == createDto.MatchId 
                        && i.ReceiverId == createDto.ReceiverId 
                        && i.Status == InviteStatus.Pending, ct);
        
        if (existingInvite)
        {
            return Result<MatchInviteDto>.Fail($"A pending invite already exists for this user and match.");
        }

        MatchInvite invite = createDto.ToEntity();
        await dbContext.MatchInvites.AddAsync(invite, ct);
        await dbContext.SaveChangesAsync(ct);

        // Send notification to the receiver
        string matchName = $"{match.Sport} on {match.MatchDateTimeUtc:dd/MM/yyyy 'at' HH:mm}";
        
        string? sender = await dbContext.Users
            .Where(u => u.Id == createDto.SenderId)
            .Select(u => u.DisplayName ?? u.UserName!)
            .FirstOrDefaultAsync(ct);
        
        var notification = new CreateNotificationDto
        {
            Type = NotificationType.MatchInviteReceived,
            ReceiverUserId = createDto.ReceiverId,
            SenderUserId = createDto.SenderId,
            RelatedEntityId = match.Id,
            RelatedEntityName = matchName,
            Title = "Match invite",
            Message = $"{sender} invited you to the match {matchName}",
            ActionUrl = $"/matches/{match.Id}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<MatchInviteDto>> UpdateInvite(UpdateMatchInviteDto updateDto, string userId, CancellationToken ct = default)
    {
        ValidationResult validationResult = await updateInviteValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<MatchInviteDto>.Fail(validationResult.ToString());
        }

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext.MatchInvites
            .FirstOrDefaultAsync(i => i.Id == updateDto.Id, ct);

        if (invite == null)
        {
            return Result<MatchInviteDto>.Fail($"Invite with id {updateDto.Id} not found.");
        }

        // Only sender can update the invite
        if (invite.SenderId != userId)
        {
            return Result<MatchInviteDto>.Fail("Only the sender can update the invite.");
        }

        invite.UpdateEntity(updateDto);
        await dbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<bool>> DeleteInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext.MatchInvites
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
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<MatchInviteDto>> AcceptInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext.MatchInvites
            .Include(i => i.Match)
                .ThenInclude(m => m!.Participants)
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<MatchInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only receiver can accept the invite
        if (invite.ReceiverId != userId)
        {
            return Result<MatchInviteDto>.Fail("Only the receiver can accept the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<MatchInviteDto>.Fail($"Cannot accept an invite with status {invite.Status}.");
        }

        if (invite.IsExpired)
        {
            invite.Status = InviteStatus.Expired;
            await dbContext.SaveChangesAsync(ct);
            return Result<MatchInviteDto>.Fail("The invite has expired.");
        }

        // Check if match still has space
        if (invite.Match!.Participants.Count >= invite.Match.maxPlayers)
        {
            return Result<MatchInviteDto>.Fail("The match is already full.");
        }

        // Add user to match participants
        ApplicationUser? user = await dbContext.Users.FindAsync([userId], ct);
        if (user == null)
        {
            return Result<MatchInviteDto>.Fail($"User with id {userId} not found.");
        }

        invite.Match.Participants.Add(user);
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAtUtc = DateTime.UtcNow;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<MatchInviteDto>> DeclineInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext.MatchInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<MatchInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only receiver can decline the invite
        if (invite.ReceiverId != userId)
        {
            return Result<MatchInviteDto>.Fail("Only the receiver can decline the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<MatchInviteDto>.Fail($"Cannot decline an invite with status {invite.Status}.");
        }

        invite.Status = InviteStatus.Declined;
        invite.DeclinedAtUtc = DateTime.UtcNow;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }

    public async Task<Result<MatchInviteDto>> CancelInvite(string inviteId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        MatchInvite? invite = await dbContext.MatchInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId, ct);

        if (invite == null)
        {
            return Result<MatchInviteDto>.Fail($"Invite with id {inviteId} not found.");
        }

        // Only sender can cancel the invite
        if (invite.SenderId != userId)
        {
            return Result<MatchInviteDto>.Fail("Only the sender can cancel the invite.");
        }

        if (invite.Status != InviteStatus.Pending)
        {
            return Result<MatchInviteDto>.Fail($"Cannot cancel an invite with status {invite.Status}.");
        }

        invite.Status = InviteStatus.Cancelled;
        invite.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return await GetInviteById(invite.Id, ct);
    }
}
