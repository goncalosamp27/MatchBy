using MatchBy.Data;
using MatchBy.Data.Seeders;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Extensions;

public static class DatabaseExtensions
{
    public static async Task RecreateDatabase(this WebApplication app)
    {
        // 1. Go to the service container of our app, creating temporarily
        using IServiceScope scope = app.Services.CreateScope();
        
        // 2. Get the database service within the scope and from the service provider of our app
        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }
        catch (System.Exception e)
        {
            app.Logger.LogError(e, "An error occurred while recreating the database.");
            throw;
        }
    }
    
    // Extension method of WebApplication to add the ApplyMigrationsAsync functionality to the app (since we use 'this').
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // 1. Go to the service container of our app, creating temporarily
        using IServiceScope scope = app.Services.CreateScope();
        
        // 2. Get the database service within the scope and from the service provider of our app
        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // 3. Apply migrations
        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("MatchBy - Database migrations applied successfully.");
        }
        catch (System.Exception e)
        {
            app.Logger.LogError(e, "An error occurred while migrating the database.");
            throw;
        }
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        await using ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            ApplicationSeeder seeder = scope.ServiceProvider.GetRequiredService<ApplicationSeeder>();
            await seeder.SeedAsync(db, scope.ServiceProvider, CancellationToken.None);
        }
        catch (System.Exception e)
        {
            app.Logger.LogError(e, "An error occurred while seeding the database.");
        }
    }
}
