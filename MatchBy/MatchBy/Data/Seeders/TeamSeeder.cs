using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class TeamSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.Teams.Any())
        {
            return Task.CompletedTask;
        }
        var users = db.Users.ToList();
        if (users.Count < 3)
        {
            return Task.CompletedTask;
        }

        db.Teams.Add(new Team
        {
            Id = $"team_{Guid.CreateVersion7()}",
            Name = "Alpha Team",
            Description = "The best team in the league.",
            OwnerId = users[1].Id,
            Members = users,
            CreatedAtUtc = DateTime.UtcNow
        });
        
        return db.SaveChangesAsync(ct);
    }
}
