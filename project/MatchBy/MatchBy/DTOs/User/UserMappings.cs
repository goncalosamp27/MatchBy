using MatchBy.Models;

namespace MatchBy.DTOs.User;

public static class UserMappings
{
    public static UserDto ToDto(this ApplicationUser user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            DisplayName = user.DisplayName,
            AvatarUrl = user.ProfileImage == null ? "/images/user-avatar.svg" : user.ProfileImage.Url,
            PlayerRating = user.Rating,
            PreferredSports = [..user.PreferredSports],
            JoinedMatchesCount = user.JoinedMatches.Count,
            Bio = user.Bio
        };
    }
}
