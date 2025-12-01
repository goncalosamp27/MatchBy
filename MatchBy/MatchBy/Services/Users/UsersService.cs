using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Users;

public class UsersService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IImageRefreshService imageRefreshService) : IUsersService
{
    /// <summary>
    /// Retrieves a paginated list of users matching the search query.
    /// </summary>
    /// <param name="q">Search query to filter users by username or display name (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 5).</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with a list of users, ordered by username.
    /// Profile images are refreshed before returning.
    /// </returns>
    public async Task<Result<PaginationResponse<List<ApplicationUser>>>> GetUsers(
        string q, int page = 1, int pageSize = 5, CancellationToken ct = default)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        IQueryable<ApplicationUser> query = dbContext.Users
            .AsNoTracking()
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
            await imageRefreshService.RefreshUserProfileImageAsync(u);
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


    /// <summary>
    /// Retrieves a specific user by their unique identifier and refreshes their profile image.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// The user entity if found, or null if the user does not exist. The user's profile image is refreshed before returning.
    /// </returns>
    public async Task<ApplicationUser?> GetUser(string userId, CancellationToken cancellationToken)
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ApplicationUser? user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            return null;
        }
        
        await imageRefreshService.RefreshUserProfileImageAsync(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
    

}
