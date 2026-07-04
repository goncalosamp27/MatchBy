using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class TeamInviteSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.TeamInvites.Any())
        {
            return Task.CompletedTask;
        }
        
        Team? team = db.Teams.FirstOrDefault();
        var users = db.Users.ToList();
        
        if(team == null || users.Count < 3)
        {
            return Task.CompletedTask;
        }

        db.TeamInvites.Add(new TeamInvite
        {
            Id = $"teamInvite_{Guid.CreateVersion7()}",
            TeamId = team.Id,
            SenderId = users[1].Id,
            ReceiverId = users[2].Id,
            Content = "Join my team!",
            CreatedAtUtc = DateTime.UtcNow
        });
        
        return db.SaveChangesAsync(ct);
    }
}
