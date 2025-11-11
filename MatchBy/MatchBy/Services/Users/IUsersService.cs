using MatchBy.Models;

namespace MatchBy.Services.Users;

public interface IUsersService
{
    Task<PagedResult<ApplicationUser>> GetUsers(string q, int page=1, int pageSize=5, CancellationToken ct = default);
    Task<ApplicationUser?> GetUser(string userId, CancellationToken cancellationToken);
}
