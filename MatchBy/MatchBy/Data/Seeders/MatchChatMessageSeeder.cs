using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class MatchChatMessageSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.MatchChatMessages.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        Match? match = db.Matches.FirstOrDefault();
        if(match == null || users.Count < 3)
        {
            return Task.CompletedTask;
        }

        db.MatchChatMessages.Add(new MatchChatMessage
        {
            Id = $"matchChatMessage_{Guid.CreateVersion7()}",
            MatchId = match.Id,
            SenderId = users[1].Id,
            Receivers = users,
            Content = "Looking forward to the match!",
            Status = MessageStatus.Available,
            CreatedAtUtc = DateTime.UtcNow
        });
        
        return db.SaveChangesAsync(ct);
    }
}
