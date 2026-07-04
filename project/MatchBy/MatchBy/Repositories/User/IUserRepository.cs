using MatchBy.Data;
using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.Repositories.User;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<PaginationResponse<List<ApplicationUser>>> GetUsers( ApplicationDbContext dbContext, string? search = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> userIds, ApplicationDbContext dbContext, CancellationToken ct = default);
}