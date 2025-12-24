using MatchBy.DTOs.User;
using MatchBy.Models;

namespace MatchBy.Services.Users;

public interface IUsersService
{
    Task<Result<UserDto>> GetUser(string userId, CancellationToken cancellationToken = default);

    Task<Result<PaginationResponse<List<UserDto>>>> GetUsers(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}