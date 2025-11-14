using Amazon.S3;
using MatchBy.Components;
using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.Users;

public class UsersService(ApplicationDbContext applicationDbContext, IS3Service s3Service, IOptions<S3Settings> s3Settings) : IUsersService
{
    public async Task<Result<PaginationResponse<List<ApplicationUser>>>> GetUsers(
        string q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        IQueryable<ApplicationUser> query = applicationDbContext.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Where(u =>
                u.UserName!.ToLower().Contains(q.ToLower()) ||
                 u.DisplayName.ToLower().Contains(q.ToLower())
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
        
        return Result<PaginationResponse<List<ApplicationUser>>>.Ok(
            new PaginationResponse<List<ApplicationUser>>
            {
                Data = users,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
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
        Result<string> url = await s3Service.GetPresignedUrlAsync(
            $"users/{user.Id}/profile-pictures/{user.ProfileImage.Key}",
            HttpVerb.GET
        );

        if (!url.Success)
        {
            return;
        }

        user.ProfileImage = user.ProfileImage with { Url = url.Data!, ExpireDateTimeUtc = DateTime.UtcNow.AddMinutes(s3Settings.Value.DefaultUrlExpiry) };
    }
}
