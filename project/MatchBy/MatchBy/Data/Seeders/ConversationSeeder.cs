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
        var teams = db.Teams.ToList();
        if (match == null || users.Count < 3 || teams.Count < 8 )
        {
            return Task.CompletedTask;
        }
        
        List<Conversation> conversations =
        [
            new()
            {
                Id = $"conversation_{Guid.CreateVersion7()}",
                Type = ConversationType.Private,
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
                Title = "Team Alpha",
                TeamId = teams[0].Id,
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
                Title = "Team Betha",
                TeamId = teams[1].Id,
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
                Title = "Team C",
                TeamId = teams[2].Id,
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
                Title = "Team D",
                TeamId = teams[3].Id,
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
                Title = "Team E",
                TeamId = teams[4].Id,
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
                Title = "Team F",
                TeamId = teams[5].Id,
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
                Title = "Team G",
                TeamId = teams[6].Id,
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
                Title = "Team H",
                TeamId = teams[7].Id,
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
                Title = "Match Pioledo",
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
        foreach (Team team in teams)
        {
            team.ConversationId = conversations.FirstOrDefault(c => c.TeamId == team.Id)?.Id;
        }
        return db.SaveChangesAsync(ct);
    }
}
