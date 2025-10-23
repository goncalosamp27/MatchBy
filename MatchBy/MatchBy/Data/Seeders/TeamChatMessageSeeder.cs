using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class TeamChatMessageSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.MatchInvites.Any())
        {
            return Task.CompletedTask;
        }
        
        var users = db.Users.ToList();
        Team? team = db.Teams.FirstOrDefault();
        if (users.Count < 3 || team == null)
        {
            return Task.CompletedTask;
        }

        db.TeamChatMessages.Add(new TeamChatMessage
        {
            Id = $"teamChatMessage_{Guid.CreateVersion7()}",
            SenderId = users[1].Id,
            Receivers = users,
            TeamId = team.Id,
            Content = "Looking forward to the match!",
            Status = MessageStatus.Available,
            CreatedAtUtc = DateTime.UtcNow
        });
        
        return db.SaveChangesAsync(ct);
    }
}
