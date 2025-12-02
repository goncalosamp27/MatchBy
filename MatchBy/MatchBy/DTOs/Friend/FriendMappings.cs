using MatchBy.DTOs.User;

namespace MatchBy.DTOs.Friend;

public static class FriendMappings
{
    public static FriendDto ToDto(this Models.Friend friend)
    {
        return new FriendDto
        {
            Id = friend.Id,
            SenderId = friend.SenderId,
            Sender = friend.Sender?.ToDto(),
            ReceiverId = friend.ReceiverId,
            Receiver = friend.Receiver?.ToDto(),
            CreatedAtUtc = friend.CreatedAtUtc,
            UpdatedAtUtc = friend.UpdatedAtUtc,
            DeletedAtUtc = friend.DeletedAtUtc,
            Status = friend.Status
        };
    }

    public static Models.Friend ToEntity(this CreateFriendDto createDto)
    {
        return new Models.Friend
        {
            Id = $"friend_{Guid.CreateVersion7()}",
            SenderId = createDto.SenderId,
            ReceiverId = createDto.ReceiverId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = Enums.FriendStatus.Pending
        };
    }

    public static void UpdateEntity(this Models.Friend friend)
    {
        // Friend relationships typically don't need updates beyond creation and deletion
        friend.UpdatedAtUtc = DateTime.UtcNow;
    }
}



