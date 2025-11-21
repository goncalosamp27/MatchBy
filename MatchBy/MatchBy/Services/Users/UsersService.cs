using MatchBy.Data;
using MatchBy.Models;
using MatchBy.Services.ImageRefresh;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Users;

public class UsersService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IImageRefreshService imageRefreshService) : IUsersService
{
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
