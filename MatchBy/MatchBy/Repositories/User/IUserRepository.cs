using MatchBy.Data;
using MatchBy.Models;

namespace MatchBy.Repositories.User;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(string userId, ApplicationDbContext dbContext, CancellationToken ct = default);
    Task<List<ApplicationUser>> GetUsersByIdsAsync(List<string> userIds, ApplicationDbContext dbContext, CancellationToken ct = default);
    void Add(ApplicationUser entity, ApplicationDbContext dbContext);
    void Update(ApplicationUser entity, ApplicationDbContext dbContext);
    void Remove(ApplicationUser entity, ApplicationDbContext dbContext);
}