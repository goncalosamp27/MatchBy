using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Extensions;

public static class ApplicationUserExtensions
{
    public static ApplicationUser InitializeNewUser(this ApplicationUser user, string displayName)
    {
        user.DisplayName = displayName;
        user.Rating = 0;
        user.CreatedAtUtc = DateTime.UtcNow;
        return user;
    }
}
