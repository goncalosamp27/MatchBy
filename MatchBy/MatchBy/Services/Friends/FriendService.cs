using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Friend;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using MatchBy.DTOs.Notification;
using MatchBy.Services.Notifications;


namespace MatchBy.Services.Friends;

public class FriendService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateFriendDto> createFriendValidator,
    INotificationService notificationService) : IFriendService
{
    public async Task<Result<FriendDto>> GetFriendshipById(string friendshipId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Friend? friend = await dbContext
            .Friends
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        return friend == null
            ? Result<FriendDto>.Fail($"Friendship with id {friendshipId} not found.")
            : Result<FriendDto>.Ok(friend.ToDto());
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetUserFriends(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Friend> query = dbContext
            .Friends
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == FriendStatus.Accepted && f.DeletedAtUtc == null);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var friendDtos = friends.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsSent(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Friend> query = dbContext
            .Friends
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => f.SenderId == userId && f.Status == FriendStatus.Pending && f.DeletedAtUtc == null);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var friendDtos = friends.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsReceived(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<Friend> query = dbContext
            .Friends
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => f.ReceiverId == userId && f.Status == FriendStatus.Pending && f.DeletedAtUtc == null);

        int total = await query.CountAsync(ct);
        int totalPages = (int)Math.Ceiling((double)total / pageSize);

        List<Friend> friends = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var friendDtos = friends.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                NextPageAvailable = page < totalPages,
                Page = page,
                PageSize = pageSize,
                PreviousPageAvailable = page > 1,
                TotalCount = total
            });
    }

    public async Task<Result<FriendDto>> CreateFriendRequest(CreateFriendDto createDto, CancellationToken ct = default)
    {
        ValidationResult validationResult = await createFriendValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            return Result<FriendDto>.Fail(validationResult.ToString());
        }
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);


        // Check if sender exists
        bool senderExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.SenderId, ct);
        if (!senderExists)
        {
            return Result<FriendDto>.Fail($"Sender with id {createDto.SenderId} not found.");
        }

        // Check if receiver exists
        bool receiverExists = await dbContext.Users.AnyAsync(u => u.Id == createDto.ReceiverId, ct);
        if (!receiverExists)
        {
            return Result<FriendDto>.Fail($"Receiver with id {createDto.ReceiverId} not found.");
        }

        // Check if friendship already exists (in either direction)
        bool existingFriendship = await dbContext.Friends
            .AnyAsync(f => f.SenderId == createDto.SenderId && f.ReceiverId == createDto.ReceiverId ||
                          f.SenderId == createDto.ReceiverId && f.ReceiverId == createDto.SenderId, ct);

        if (existingFriendship)
        {
            return Result<FriendDto>.Fail("A friendship or friend request already exists between these users.");
        }

        Friend friend = createDto.ToEntity();
        await dbContext.Friends.AddAsync(friend, ct);
        await dbContext.SaveChangesAsync(ct);

        var sender = await dbContext.Users
        .AsNoTracking()
        .FirstAsync(u => u.Id == friend.SenderId, ct);

        var senderName = sender.DisplayName ?? sender.UserName ?? "Someone";

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.FriendRequestReceived,
            ReceiverUserId = friend.ReceiverId,
            SenderUserId = friend.SenderId,
            RelatedEntityId = friend.Id,
            RelatedEntityName = "Friendship",
            Title = "You received a friend request",
            Message = $"{senderName} wants to be your friend!",
            ActionUrl = $"/profile/{friend.SenderId}"
        };

        await notificationService.SendNotificationAsync(notification, ct);        

        return await GetFriendshipById(friend.Id, ct);
    }

    public async Task<Result<FriendDto>> AcceptRequest(string friendshipId, string receiverId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var friend = await dbContext.Friends.FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friend == null || friend.DeletedAtUtc != null) {
            return Result<FriendDto>.Fail("Request not found.");
        }

        if (friend.ReceiverId != receiverId) {
            return Result<FriendDto>.Fail("Only the receiver can accept this request.");
        }

        friend.Status = FriendStatus.Accepted;
        friend.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        var receiver = await dbContext.Users
        .AsNoTracking()
        .FirstAsync(u => u.Id == friend.ReceiverId, ct);

        var receiverName = receiver.DisplayName ?? receiver.UserName ?? "Someone";

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.FriendRequestAccepted,   
            ReceiverUserId = friend.SenderId,                
            SenderUserId = friend.ReceiverId,               
            RelatedEntityId = friend.Id,
            RelatedEntityName = "Friendship",
            Title = "Friend request accepted",
            Message = $"{receiverName} is your new friend!",
            ActionUrl = $"/profile/{friend.ReceiverId}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

            return Result<FriendDto>.Ok(friend.ToDto());
    }

    public async Task<Result<bool>> RejectRequest(string friendshipId, string receiverId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var friend = await dbContext.Friends.FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friend == null || friend.DeletedAtUtc != null) {
            return Result<bool>.Fail("Request not found.");
        }

        if (friend.ReceiverId != receiverId) {
            return Result<bool>.Fail("Only the receiver can reject this request.");
        }
        
        friend.DeletedAtUtc = DateTime.UtcNow;
        friend.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemoveFriend(string friendshipId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await dbContext.Friends
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friend == null)
        {
            return Result<bool>.Fail($"Friendship with id {friendshipId} not found.");
        }

        if (friend.SenderId != userId && friend.ReceiverId != userId)
        {
            return Result<bool>.Fail("Only the users involved in the friendship can remove it.");
        }

        dbContext.Friends.Remove(friend);
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CheckFriendship(string userId1, string userId2, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        bool areFriends = await dbContext.Friends
            .AnyAsync(f => f.DeletedAtUtc == null && f.Status == Enums.FriendStatus.Accepted && ((f.SenderId == userId1 && f.ReceiverId == userId2) ||
                          (f.SenderId == userId2 && f.ReceiverId == userId1)), ct);

        return Result<bool>.Ok(areFriends);
    }

    public async Task<Result<FriendDto?>> GetFriendshipBetweenUsers(string userId1, string userId2, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var friend = await dbContext.Friends
            .AsNoTracking()
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f =>
                f.DeletedAtUtc == null &&
                (
                    (f.SenderId == userId1 && f.ReceiverId == userId2) ||
                    (f.SenderId == userId2 && f.ReceiverId == userId1)
                ),
                ct);

        return Result<FriendDto?>.Ok(friend?.ToDto());
    }

    public async Task<Result<bool>> CancelFriendRequest(string friendshipId, string senderId, CancellationToken ct = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var friend = await dbContext.Friends
            .FirstOrDefaultAsync(f => f.Id == friendshipId, ct);

        if (friend == null || friend.DeletedAtUtc != null)
        {
            return Result<bool>.Fail("Friend request not found.");
        }

        if (friend.SenderId != senderId)
        {
            return Result<bool>.Fail("Only the user who sent the request can cancel it.");
        }

        if (friend.Status != FriendStatus.Pending)
        {
            return Result<bool>.Fail("Only pending friend requests can be cancelled.");
        }

        dbContext.Friends.Remove(friend);
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
}