using FluentValidation;
using FluentValidation.Results;
using MatchBy.Data;
using MatchBy.DTOs.Friend;
using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;
using MatchBy.DTOs.Notification;
using MatchBy.Repositories.Friend;
using MatchBy.Repositories.User;
using MatchBy.Services.Notifications;

namespace MatchBy.Services.Friends;

public class FriendService(
    IUserRepository userRepository,
    IFriendRepository friendRepository,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IValidator<CreateFriendDto> createFriendValidator,
    INotificationService notificationService) : IFriendService
{
    public async Task<Result<FriendDto>> GetFriendshipById(string friendshipId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Friend? friend = await friendRepository.GetByIdAsync(friendshipId, dbContext, ct);

        return friend == null
            ? Result<FriendDto>.Fail($"Friendship with id {friendshipId} not found.")
            : Result<FriendDto>.Ok(friend.ToDto());
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetUserFriends(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        PaginationResponse<List<Friend>> friends = await friendRepository.GetUserFriends(userId, dbContext, page, pageSize, ct);

        var friendDtos = friends.Data.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                Page = friends.Page,
                PageSize = friends.PageSize,
                TotalCount = friends.TotalCount
            });
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsSent(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        PaginationResponse<List<Friend>> friends = await friendRepository.GetFriendRequestsSent(userId, dbContext, page, pageSize, ct);

        var friendDtos = friends.Data.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                Page = friends.Page,
                PageSize = friends.PageSize,
                TotalCount = friends.TotalCount
            });
    }

    public async Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsReceived(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        
        PaginationResponse<List<Friend>> friends = await friendRepository.GetFriendRequestsReceived(userId, dbContext, page, pageSize, ct);

        var friendDtos = friends.Data.Select(f => f.ToDto()).ToList();

        return Result<PaginationResponse<List<FriendDto>>>.Ok(
            new PaginationResponse<List<FriendDto>>
            {
                Data = friendDtos,
                Page = friends.Page,
                PageSize = friends.PageSize,
                TotalCount = friends.TotalCount
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
        ApplicationUser? sender = await userRepository.GetByIdAsync(createDto.SenderId, dbContext, ct);
        if (sender == null)
        {
            return Result<FriendDto>.Fail($"Sender with id {createDto.SenderId} not found.");
        }

        // Check if receiver exists
        ApplicationUser receiver = await userRepository.GetByIdAsync(createDto.ReceiverId, dbContext, ct);
        if (receiver == null)
        {
            return Result<FriendDto>.Fail($"Receiver with id {createDto.ReceiverId} not found.");
        }

        // Check if friendship already exists
        Friend? existingFriendship = await friendRepository.ExistsAsync(createDto.SenderId, createDto.ReceiverId, dbContext, ct);
        if (existingFriendship != null)
        {
            return Result<FriendDto>.Fail("A friendship or friend request already exists between these users.");
        }

        Friend friend = createDto.ToEntity();
        
        friendRepository.Add(friend, dbContext);
        await dbContext.SaveChangesAsync(ct);

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.Friend,
            ReceiverUserId = friend.ReceiverId,
            SenderUserId = friend.SenderId,
            RelatedEntityId = friend.Id,
            RelatedEntityName = "Friendship",
            Title = "You received a friend request",
            Message = $"{sender.DisplayName} wants to be your friend!",
            ActionUrl = $"/profile/{friend.SenderId}"
        };

        await notificationService.SendNotificationAsync(notification, ct);        

        return await GetFriendshipById(friend.Id, ct);
    }

    public async Task<Result<FriendDto>> AcceptRequest(string friendshipId, string receiverId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await friendRepository.GetByIdAsync(friendshipId, dbContext, ct);
        if (friend == null) {
            return Result<FriendDto>.Fail("Friend request not found.");
        }

        if (friend.ReceiverId != receiverId) {
            return Result<FriendDto>.Fail("Only the receiver can accept this request.");
        }
        
        if(friend.Receiver == null) {
            return Result<FriendDto>.Fail("Receiver user not found.");
        }
        
        if(friend.Status == FriendStatus.Accepted) {
            return Result<FriendDto>.Fail("Friend request has already been accepted.");
        }

        friend.Status = FriendStatus.Accepted;
        friend.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        var notification = new CreateNotificationDto
        {
            Type = NotificationType.Friend,   
            ReceiverUserId = friend.SenderId,                
            SenderUserId = friend.ReceiverId,               
            RelatedEntityId = friend.Id,
            RelatedEntityName = "Friendship",
            Title = "Friend request accepted",
            Message = $"{friend.Receiver.DisplayName} is your new friend!",
            ActionUrl = $"/profile/{friend.ReceiverId}"
        };

        await notificationService.SendNotificationAsync(notification, ct);

        return Result<FriendDto>.Ok(friend.ToDto());
    }

    public async Task<Result<bool>> RejectRequest(string friendshipId, string receiverId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await friendRepository.GetByIdAsync(friendshipId, dbContext, ct);
        if (friend is null) {
            return Result<bool>.Fail("Request not found.");
        }

        if (friend.ReceiverId != receiverId) {
            return Result<bool>.Fail("Only the receiver can reject this request.");
        }
        
        friendRepository.Remove(friend, dbContext);
        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemoveFriend(string friendshipId, string userId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await friendRepository.GetByIdAsync(friendshipId, dbContext, ct);

        if (friend == null)
        {
            return Result<bool>.Fail($"Friendship with id {friendshipId} not found.");
        }

        if (friend.SenderId != userId && friend.ReceiverId != userId)
        {
            return Result<bool>.Fail("Only the users involved in the friendship can remove it.");
        }
        
        if(friend.Status == FriendStatus.Pending) {
            return Result<bool>.Fail("Cannot remove a pending friend request. Please reject the request instead.");
        }

        dbContext.Friends.Remove(friend);
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CheckFriendship(string userId1, string userId2, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await friendRepository.GetByIdAsync(userId1, dbContext, ct);
        
        return friend == null ? Result<bool>.Ok(false) : Result<bool>.Ok(friend.Status == FriendStatus.Accepted);
    }

    public async Task<Result<FriendDto>> GetFriendshipBetweenUsers(string userId1, string userId2, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
        Friend? friend = await friendRepository.ExistsAsync(userId1, userId2, dbContext, ct);
        return friend is null ? Result<FriendDto>.Fail("No friendship exists between the specified users.") : Result<FriendDto>.Ok(friend.ToDto());
    }

    public async Task<Result<bool>> CancelFriendRequest(string friendshipId, string senderId, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        Friend? friend = await friendRepository.GetByIdAsync(friendshipId, dbContext, ct);
        if (friend == null)
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

        friendRepository.Remove(friend, dbContext);
        await dbContext.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }
}