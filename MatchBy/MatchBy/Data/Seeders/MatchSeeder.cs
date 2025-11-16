using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Data.Seeders;


public class MatchSeeder : ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.Matches.Any())
        {
            return Task.CompletedTask;
        }
        
        var users = db.Users.ToList();
        if (users.Count < 3)
        {
            return Task.CompletedTask;
        }
        
        db.Matches.Add(new Match
        {
            Id = $"match_{Guid.CreateVersion7()}",
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(7),
            Description = "Sunday Soccer Match",
            minPlayers = 5,
            maxPlayers = 10,
            Sport = Sports.Football,
            Status = MatchStatus.Confirmed,
            Privacy = MatchPrivacy.Public,
            CreatorId = users[1].Id,
            Participants = users,
            CreatedAtUtc = DateTime.UtcNow,
            Address = "Match Address"
        });
        
        return db.SaveChangesAsync(ct);
    }
}
