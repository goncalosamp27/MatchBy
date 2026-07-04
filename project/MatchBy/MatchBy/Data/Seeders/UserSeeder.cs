﻿using MatchBy.Enums;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity;

namespace MatchBy.Data.Seeders;

public class UserSeeder(ILogger<UserSeeder> logger) : ISeeder
{
    public async Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        UserManager<ApplicationUser> userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole> rolesManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        if (!rolesManager.Roles.Any())
        {
            await rolesManager.CreateAsync(new IdentityRole(Roles.Admin));
            await rolesManager.CreateAsync(new IdentityRole(Roles.Member));
        }

        if (userManager.Users.Any())
        {
            return;
        }

        // Create admin user
        await CreateUserIfNotExists(userManager, "admin@admin.com", "admin@admin.com", "Admin!123", Roles.Admin);
        
        // Create regular test users
        await CreateUserIfNotExists(userManager, "user1@user.com", "user1@user.com", "User1!123", Roles.Member);
        await CreateUserIfNotExists(userManager, "user2@user.com", "user2@user.com", "User2!123", Roles.Member);
        
        // Create test user for Playwright account deletion tests
        await CreateUserIfNotExists(userManager, "test1@test.com", "test1@test.com", "Test!123", Roles.Member);
        await CreateUserIfNotExists(userManager, "test2@test.com", "test2@test.com", "Test!123", Roles.Member);

        


    }

    private async Task CreateUserIfNotExists(
        UserManager<ApplicationUser> userManager,
        string username,
        string email,
        string password,
        string role)
    {
        ApplicationUser? existing = await userManager.FindByNameAsync(username);
        if (existing != null)
        {
            logger.LogInformation("User seeding skipped — users already exist in the database.");
            return;
        }

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            DisplayName = username,
            PreferredSports = [Sports.Football, Sports.Basketball],
            Rating = 5.0f,
            CreatedAtUtc = DateTime.UtcNow
        };

        IdentityResult result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            logger.LogInformation("User {UserName} created successfully.", username);
            if (role == Roles.Admin)
            {
                await userManager.AddToRoleAsync(user, Roles.Admin);
            }
            else
            {
                await userManager.AddToRoleAsync(user, Roles.Member);
            }
        }
        else
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create user {UserName}: {Errors}", username, errors);
        }
    }
}
