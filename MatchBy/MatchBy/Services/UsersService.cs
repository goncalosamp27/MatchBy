using MatchBy.Data;
using MatchBy.Models;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Services;

public class UsersService(ApplicationDbContext applicationDbContext): IUsersService
{
    public async Task<List<ApplicationUser>> GetUsers()
    {
        return await applicationDbContext.Users.ToListAsync();
    }
}
