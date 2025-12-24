using MatchBy.Models;

namespace MatchBy.Services.ImageRefresh;

/// <summary>
/// Service for refreshing expired S3 presigned URLs for images.
/// </summary>
public interface IImageRefreshService
{
    /// <summary>
    /// Refreshes the profile image URL for a user if it has expired.
    /// </summary>
    /// <param name="user">The user whose profile image should be refreshed.</param>
    Task RefreshUserProfileImageAsync(ApplicationUser user);

    /// <summary>
    /// Refreshes the team image URL if it has expired.
    /// </summary>
    /// <param name="team">The team whose image should be refreshed.</param>
    Task RefreshTeamImageAsync(Team team);

    /// <summary>
    /// Refreshes the team image, owner image and all members profile images.
    /// </summary>
    /// <param name="team">The team to refresh images for.</param>
    Task RefreshTeamImagesAsync(Team team);

    /// <summary>
    /// Refreshes the conversation image URL if it has expired.
    /// </summary>
    /// <param name="conversation">The conversation whose image should be refreshed.</param>
    Task RefreshConversationImageAsync(Conversation conversation);

    /// <summary>
    /// Refreshes the conversation image and all participant profile images.
    /// </summary>
    /// <param name="conversation">The conversation to refresh images for.</param>
    Task RefreshConversationImagesAsync(Conversation conversation);

    /// <summary>
    /// Refreshes the notification related images such as sender and receiver profile images.
    /// </summary>
    /// <param name="notification">The notification to refresh images for.</param>
    Task RefreshNotificationImagesAsync(Notification notification);
}
