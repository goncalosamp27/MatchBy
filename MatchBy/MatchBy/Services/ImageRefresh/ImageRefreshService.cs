using Amazon.S3;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.ImageRefresh;

/// <summary>
/// Centralized service for refreshing expired S3 presigned URLs for images.
/// </summary>
public class ImageRefreshService(
    IS3Service s3Service,
    IOptions<S3Settings> s3Settings) : IImageRefreshService
{
    /// <summary>
    /// Refreshes the presigned URL for a user's profile image if it has expired or is missing.
    /// </summary>
    /// <param name="user">The user entity whose profile image should be refreshed.</param>
    /// <remarks>
    /// This method checks if the user has a profile image and if the current URL has expired.
    /// If expired or missing, it generates a new presigned URL from S3 and updates the user's profile image metadata.
    /// </remarks>
    public async Task RefreshUserProfileImageAsync(ApplicationUser user)
    {
        if (user.ProfileImage?.Key is null)
        {
            return;
        }

        if (user.ProfileImage.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(user.ProfileImage.Url))
        {
            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}", HttpVerb.GET);

            if (url.Success)
            {
                user.ProfileImage = user.ProfileImage with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }
    }

    /// <summary>
    /// Refreshes the presigned URL for a team's image if it has expired or is missing.
    /// </summary>
    /// <param name="team">The team entity whose image should be refreshed.</param>
    /// <remarks>
    /// This method checks if the team has an image and if the current URL has expired.
    /// If expired or missing, it generates a new presigned URL from S3 and updates the team's image metadata.
    /// </remarks>
    public async Task RefreshTeamImageAsync(Team team)
    {
        if (team.Image?.Key is null)
        {
            return;
        }

        if (team.Image.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(team.Image.Url))
        {
            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"teams/{team.Id}/image/{team.Image.Key}", HttpVerb.GET);

            if (url.Success)
            {
                team.Image = team.Image with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }
        }
    }
    
    /// <summary>
    /// Refreshes all images associated with a team, including the team image, member profile images, owner profile image, and conversation image.
    /// </summary>
    /// <param name="team">The team entity whose images should be refreshed.</param>
    /// <remarks>
    /// This method refreshes images in parallel for better performance. It refreshes:
    /// - The team's own image
    /// - All team members' profile images
    /// - The team owner's profile image
    /// - The associated conversation image (if exists)
    /// </remarks>
    public async Task RefreshTeamImagesAsync(Team team)
    {
        // Refresh conversation image
        await RefreshTeamImageAsync(team);

        // Refresh all participant profile images
        await Task.WhenAll(team.Members.Select(RefreshUserProfileImageAsync));

        // Refresh creator profile image
        if (team.Owner is not null)
        {
            await RefreshUserProfileImageAsync(team.Owner);
        }
        
        // Refresh team conversation image
        if (team.Conversation is not null)
        {
            await RefreshConversationImageAsync(team.Conversation);
        }
    }
    
    /// <summary>
    /// Refreshes profile images for both the sender and receiver of a notification.
    /// </summary>
    /// <param name="notification">The notification entity whose associated user images should be refreshed.</param>
    /// <remarks>
    /// This method refreshes the profile images for both the notification sender and receiver in parallel.
    /// </remarks>
    public async Task RefreshNotificationImagesAsync(Notification notification)
    {
        // Refresh receiver profile image
        if (notification.Receiver is not null)
        {
            await RefreshUserProfileImageAsync(notification.Receiver);
        }
        
        // Refresh sender profile image
        if (notification.Sender is not null)
        {
            await RefreshUserProfileImageAsync(notification.Sender);
        }
    }

    /// <summary>
    /// Refreshes the presigned URL for a conversation's image if it has expired or is missing.
    /// Also refreshes the associated team image if the conversation is linked to a team.
    /// </summary>
    /// <param name="conversation">The conversation entity whose image should be refreshed.</param>
    /// <remarks>
    /// This method checks if the conversation has an image or is linked to a team with an image.
    /// If expired or missing, it generates new presigned URLs from S3 and updates the metadata.
    /// </remarks>
    public async Task RefreshConversationImageAsync(Conversation conversation)
    {
        if (conversation.Image?.Key is null && conversation.Team?.Image is null)
        {
            return;
        }

        if (conversation.Image is not null && (conversation.Image.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(conversation.Image.Url)))
        {
            Result<string> url = await s3Service.GetPresignedUrlAsync(
                $"conversations/{conversation.Id}/image/{conversation.Image.Key}", HttpVerb.GET);

            if (url.Success)
            {
                conversation.Image = conversation.Image with
                {
                    Url = url.Data!,
                    ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry)
                };
            }  
        }
        
        if (conversation.Team?.Image != null && (conversation.Team.Image.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(conversation.Team.Image.Url)))
        {
            await RefreshTeamImageAsync(conversation.Team);
        }
    }

    /// <summary>
    /// Refreshes all images associated with a conversation, including the conversation image, participant profile images, and creator profile image.
    /// </summary>
    /// <param name="conversation">The conversation entity whose images should be refreshed.</param>
    /// <remarks>
    /// This method refreshes images in parallel for better performance. It refreshes:
    /// - The conversation's own image
    /// - All conversation participants' profile images
    /// - The conversation creator's profile image
    /// </remarks>
    public async Task RefreshConversationImagesAsync(Conversation conversation)
    {
        // Refresh conversation image
        await RefreshConversationImageAsync(conversation);

        // Refresh all participant profile images
        await Task.WhenAll(conversation.Participants.Select(RefreshUserProfileImageAsync));

        // Refresh creator profile image
        if (conversation.Creator is not null)
        {
            await RefreshUserProfileImageAsync(conversation.Creator);
        }
    }
}
