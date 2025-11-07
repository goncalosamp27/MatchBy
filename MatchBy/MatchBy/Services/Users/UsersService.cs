using Amazon.S3;
using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Users;

public class UsersService(ApplicationDbContext applicationDbContext, IS3Service s3Service, IOptions<S3Settings> s3Settings) : IUsersService
{
    public async Task<List<ApplicationUser>> GetUsers()
    {
        List<ApplicationUser> users = await applicationDbContext.Users.ToListAsync();
        await Task.WhenAll(users.Select(RefreshProfileImage));

        if (users.Any())
        {
            await applicationDbContext.SaveChangesAsync();   
        }
        
        return users;
    }

    public async Task<ApplicationUser?> GetUser(string userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await applicationDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return null;
        }
        
        await RefreshProfileImage(user);
        await applicationDbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
    
    // Refreshes the profile image URL if it has expired
    private async Task RefreshProfileImage(ApplicationUser user)
    {
        if (user.ProfileImage == null || user.ProfileImage.ExpireDateTimeUtc > DateTime.UtcNow)
        {
            return;
        }
        
        // Get profile image Url from s3Service
        string? url = await s3Service.GetPresignedUrlAsync(
            $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}",
            HttpVerb.GET
        );

        if (url is null)
        {
            return;
        }

        user.ProfileImage = user.ProfileImage with { Url = url, ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry) };
    }
}
