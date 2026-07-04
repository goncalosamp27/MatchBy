using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class FriendSeeder : ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.Friends.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        if (users.Count >= 3)
        {
            db.Friends.AddRange(
                new Friend { Id = $"friend_{Guid.CreateVersion7()}", SenderId = users[1].Id, ReceiverId = users[2].Id, CreatedAtUtc = DateTime.UtcNow }
            );
        }

        return db.SaveChangesAsync(ct);

    }
}
