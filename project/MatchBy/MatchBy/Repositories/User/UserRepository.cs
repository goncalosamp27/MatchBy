using MatchBy.Data;
using MatchBy.DTOs.User;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Repositories.User;

public class UserRepository: IUserRepository
{
    public Task<ApplicationUser?> GetByIdAsync(string userId, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
       return dbContext.Users
            .Include(u => u.JoinedMatches)
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PaginationResponse<List<ApplicationUser>>> GetUsers(ApplicationDbContext dbContext, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> query = dbContext.Users
            .Include(u => u.JoinedMatches)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(search) ||
                u.DisplayName.ToLower().Contains(search)
            );
        }

        int total = await query.CountAsync(cancellationToken);

        List<ApplicationUser> users = await query
            .OrderBy(u => u.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        
        var response = new PaginationResponse<List<ApplicationUser>>
        {
            Data = users,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return response;
    }

    public Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> userIds, ApplicationDbContext dbContext, CancellationToken ct = default)
    {
        return dbContext.Users
            .Include(u => u.JoinedMatches)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(ct);
    }
}