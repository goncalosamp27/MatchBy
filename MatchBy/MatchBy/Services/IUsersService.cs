using MatchBy.Models;

namespace MatchBy.Services;

public interface IUsersService
{
    Task<List<ApplicationUser>> GetUsers();
}
