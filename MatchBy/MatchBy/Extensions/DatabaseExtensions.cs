using MatchBy.Data;
using MatchBy.Data.Seeders;
using MatchBy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MatchBy.Extensions;

public static class DatabaseExtensions
{
    //metodo de extensao da WebApplication para adicionar a funcionalidade ApplyMigrationsAsync á app(pois usamos o 'this').
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // 1. "Vou ao armazém dos servicos da nossa app, criando temporariamente"
        using IServiceScope scope = app.Services.CreateScope();
        
        // 2. "Quero o serviço da base de dados, por favor, dentro do scope e no provedor de servicos da nossa app"
        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // 3. "Vou usar para aplicar migrações"
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
