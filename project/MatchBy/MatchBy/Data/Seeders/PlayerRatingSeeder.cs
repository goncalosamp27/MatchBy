using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class PlayerRatingSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.PlayerRatings.Any())
        {
            return Task.CompletedTask;
        }
        var users = db.Users.ToList();
        Match? match = db.Matches.FirstOrDefault();
        if(users.Count < 3 || match == null)
        {
            return Task.CompletedTask;
        }
        
        db.PlayerRatings.Add(new PlayerRating
            {
                Id = $"playerRating_{Guid.CreateVersion7()}",
                Rating = 4,
                SentById = users[1].Id,
                ReceivedById = users[2].Id,
                MatchId = match.Id,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
        
        return db.SaveChangesAsync(ct);
    }
}
