namespace MatchBy.Data.Seeders;

public sealed class ApplicationSeeder(IEnumerable<ISeeder> seeders) : ISeeder
{
    public async Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        foreach (ISeeder s in seeders)
        {
            await s.SeedAsync(db, sp, ct);
        }
    }
}
