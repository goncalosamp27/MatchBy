using MatchBy.Models;

namespace MatchBy.Services;

public interface IUsersService
{
    Task<List<ApplicationUser>> GetUsers();
    Task<ApplicationUser> GetUser(string userId, CancellationToken cancellationToken);
}
