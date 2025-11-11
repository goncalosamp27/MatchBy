using Amazon.S3;
using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Users;

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
}
public class UsersService(ApplicationDbContext applicationDbContext, IS3Service s3Service, IOptions<S3Settings> s3Settings) : IUsersService
{
    public async Task<PagedResult<ApplicationUser>> GetUsers(
        string q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<ApplicationUser> query = applicationDbContext.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Where(u =>
                u.UserName!.Contains(q) ||
                 u.DisplayName.Contains(q)
            );

        int total = await query.CountAsync(ct);

        List<ApplicationUser> users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        
        foreach (ApplicationUser u in users)
        {
            await RefreshProfileImage(u);
        }
        
        return new PagedResult<ApplicationUser>
        {
            Items = users,
            TotalCount = total
        };
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
