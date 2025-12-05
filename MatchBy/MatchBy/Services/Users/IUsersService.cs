// Services/Users/IUsersService.cs

using MatchBy.Models;

namespace MatchBy.Services.Users;

public interface IUsersService
{
    Task<Result<ApplicationUser>> GetUser(string userId, CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<List<ApplicationUser>>>> GetUsers(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}