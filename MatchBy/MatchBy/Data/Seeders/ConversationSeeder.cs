using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class ConversationSeeder : ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.Conversations.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        Match? match = db.Matches.FirstOrDefault();
        Team? team = db.Teams.FirstOrDefault();
        if (match == null || users.Count < 3 || team == null )
        {
            return Task.CompletedTask;
        }
        
        List<Conversation> conversations =
        [
            new()
            {
                Id = $"conversation_{Guid.CreateVersion7()}",
                Type = ConversationType.Private,
                Title = "Private Conversation",
                CreatorId = users[0].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Participants =
                [
                    users[0],
                    users[1]
                ]
            },
            new()
            {
                Id = $"conversation_{Guid.CreateVersion7()}",
                Type = ConversationType.Team,
                Title = "Team Conversation",
                TeamId = team.Id,
                CreatorId = users[0].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Participants =
                [
                    users[0],
                    users[1]
                ]
            },
            new()
            {
                Id = $"conversation_{Guid.CreateVersion7()}",
                Type = ConversationType.Match,
                Title = "Match Conversation",
                CreatorId = users[0].Id,
                MatchId = match.Id,
                CreatedAtUtc = DateTime.UtcNow,
                Participants =
                [
                    users[0],
                    users[1]
                ]
            }
        ];

        db.Conversations.AddRange(conversations);
        team.ConversationId = conversations[1].Id;

        return db.SaveChangesAsync(ct);
    }
}
