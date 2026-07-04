using MatchBy.Data;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Repositories.User;
using MatchBy.Services.ImageRefresh;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services.Users;

public class UsersService(
    IUserRepository userRepository,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IImageRefreshService imageRefreshService)
    : IUsersService
{
    /// <summary>
    /// Retrieves a specific user by their unique identifier and refreshes their profile image.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// The user entity if found, or null if the user does not exist. The user's profile image is refreshed before returning.
    /// </returns>
    public async Task<Result<UserDto>> GetUser(string userId, CancellationToken cancellationToken = default)
    {
        await using ApplicationDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ApplicationUser? user = await userRepository.GetByIdAsync(userId, context, cancellationToken);
        if (user == null)
        {
            return Result<UserDto>.Fail("User not found.");
        }

        await imageRefreshService.RefreshUserProfileImageAsync(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Ok(user.ToDto());
    }

    /// <summary>
    /// Retrieves a paginated list of users matching the search query.
    /// </summary>
    /// <param name="search">Search query to filter users by username or display name (case-insensitive).</param>
    /// <param name="page">The page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of users per page (default: 5).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A result containing a paginated response with a list of users, ordered by username.
    /// Profile images are refreshed before returning.
    /// </returns>
    public async Task<Result<PaginationResponse<List<UserDto>>>> GetUsers(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await using ApplicationDbContext context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        PaginationResponse<List<ApplicationUser>> users = await userRepository.GetUsers(context, search, page, pageSize, cancellationToken);

        // Refresh profile images for all users
        foreach (ApplicationUser user in users.Data)
        {
            await imageRefreshService.RefreshUserProfileImageAsync(user);
        }
        await context.SaveChangesAsync(cancellationToken);

        var userDtos = users.Data.Select(u => u.ToDto()).ToList();

        return Result<PaginationResponse<List<UserDto>>>.Ok(new PaginationResponse<List<UserDto>>
        {
            Data = userDtos,
            TotalCount = users.TotalCount,
            Page = users.Page,
            PageSize = users.PageSize
        });
    }
}