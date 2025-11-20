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

        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        
        // Team 1: Public team with multiple members
        db.Teams.Add(new Team
        {
            Id = $"team_{Guid.CreateVersion7()}",
            Name = "Alpha Team",
            Description = "The best team in the league. We play competitive matches every weekend.",
            OwnerId = users[0].Id,
            Privacy = TeamPrivacy.Public,
            Members = users.Take(3).ToList(),
            MaxMembers = 10,
            CreatedAtUtc = baseDate.AddDays(-25)
        });
        
        // Team 2: Private team with owner only
        if (users.Count > 1)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Elite Squad",
                Description = "An exclusive team for experienced players. Private matches and training sessions.",
                OwnerId = users[1].Id,
                Privacy = TeamPrivacy.Private,
                Members = new List<ApplicationUser> { users[1] },
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-20)
            });
        }
        
        // Team 3: Public team with many members
        if (users.Count > 2)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Weekend Warriors",
                Description = "Casual team for weekend matches. All skill levels welcome!",
                OwnerId = users[2 % users.Count].Id,
                Privacy = TeamPrivacy.Public,
                Members = users.Take(Math.Min(5, users.Count)).ToList(),
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-15)
            });
        }
        
        // Team 4: Private team with selected members
        if (users.Count > 3)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Champions League",
                Description = "Competitive team aiming for tournaments. Regular practice required.",
                OwnerId = users[0].Id,
                Privacy = TeamPrivacy.Private,
                Members = users.Where((u, i) => i % 2 == 0).Take(4).ToList(),
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-10)
            });
        }
        
        // Team 5: Public team - recently created
        if (users.Count > 1)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Rising Stars",
                Description = "New team looking for dedicated players. Join us on our journey!",
                OwnerId = users[1].Id,
                Privacy = TeamPrivacy.Public,
                Members = new List<ApplicationUser> { users[1], users[Math.Min(2, users.Count - 1)] },
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-5)
            });
        }
        
        // Team 6: Public team - mixed members
        if (users.Count > 4)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Thunder Bolts",
                Description = "Fast-paced team with aggressive playstyle. Looking for skilled players.",
                OwnerId = users[3 % users.Count].Id,
                Privacy = TeamPrivacy.Public,
                Members = users.Skip(2).Take(3).ToList(),
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-8)
            });
        }
        
        // Team 7: Private team - exclusive
        if (users.Count > 2)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Veterans United",
                Description = "Team for experienced players only. Years of experience required.",
                OwnerId = users[2].Id,
                Privacy = TeamPrivacy.Private,
                Members = users.Take(2).ToList(),
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-12)
            });
        }
        
        // Team 8: Public team - community focused
        if (users.Count > 1)
        {
            db.Teams.Add(new Team
            {
                Id = $"team_{Guid.CreateVersion7()}",
                Name = "Community Champions",
                Description = "Friendly team focused on fun and community. Everyone is welcome!",
                OwnerId = users[0].Id,
                Privacy = TeamPrivacy.Public,
                Members = users.Take(Math.Min(4, users.Count)).ToList(),
                MaxMembers = 10,
                CreatedAtUtc = baseDate.AddDays(-18)
            });
        }
        
        return db.SaveChangesAsync(ct);
    }
}
