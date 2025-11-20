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

    public async Task RefreshConversationImageAsync(Conversation conversation)
    {
        if (conversation.Image?.Key is null)
        {
            return;
        }

        if (conversation.Image.ExpireDateTimeUtc < DateTime.UtcNow || string.IsNullOrEmpty(conversation.Image.Url))
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
    }

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
