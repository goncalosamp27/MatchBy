using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class MatchInviteSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.MatchInvites.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        Match? match = db.Matches.FirstOrDefault();
        
        if(match == null || users.Count < 3)
        {
            return Task.CompletedTask;
        }
        
        db.MatchInvites.Add(new MatchInvite
        {
            Id = $"matchInvite_{Guid.CreateVersion7()}",
            MatchId = match.Id,
            SenderId = users[1].Id,
            ReceiverId = users[2].Id,
            Content = "Join me for the soccer match next week!",
            CreatedAtUtc = DateTime.UtcNow
        });
        
        return db.SaveChangesAsync(ct);
    }
}
