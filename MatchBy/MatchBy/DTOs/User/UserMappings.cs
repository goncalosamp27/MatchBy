using MatchBy.Models;

namespace MatchBy.DTOs.User;

public static class UserMappings
{
    public static UserDto ToDto(this ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.UserName!,
            AvatarUrl = user.ProfileImage?.Url
        };
    }
}
