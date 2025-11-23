using MatchBy.DTOs.Friend;
using MatchBy.Models;

namespace MatchBy.Services.Friends;

public interface IFriendService
{
    Task<Result<FriendDto>> GetFriendshipById(string friendshipId, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<FriendDto>>>> GetUserFriends(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsSent(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<PaginationResponse<List<FriendDto>>>> GetFriendRequestsReceived(string userId, int page = 1, int pageSize = 10, CancellationToken ct = default);
    Task<Result<FriendDto>> CreateFriendRequest(CreateFriendDto createDto, CancellationToken ct = default);
    Task<Result<FriendDto>> UpdateFriendship(UpdateFriendDto updateDto, string userId, CancellationToken ct = default);
    Task<Result<bool>> RemoveFriend(string friendshipId, string userId, CancellationToken ct = default);
    Task<Result<bool>> CheckFriendship(string userId1, string userId2, CancellationToken ct = default);
}
