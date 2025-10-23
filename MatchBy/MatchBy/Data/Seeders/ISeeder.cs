namespace MatchBy.Data.Seeders;

public interface ISeeder
{
    Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct);
}
