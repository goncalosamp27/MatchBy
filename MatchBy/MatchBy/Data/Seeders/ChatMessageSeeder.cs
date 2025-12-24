using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class ChatMessageSeeder : ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.ChatMessages.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        Match? match = db.Matches.FirstOrDefault();
        var conversations = db.Conversations.Take(3).ToList();
        if (match == null || users.Count < 3 || conversations.Count < 3)
        {
            return Task.CompletedTask;
        }

        var messages = new List<ChatMessage>
        {
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                Content = "Hello! Excited for the match.",
                SenderId = users[1].Id,
                ConversationId = conversations[0].Id,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                Content = "Hello! Excited for the match.",
                SenderId = users[1].Id,
                ConversationId = conversations[1].Id,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                Content = "Hello! Excited for the match.",
                SenderId = users[1].Id,
                ConversationId = conversations[2].Id,
                CreatedAtUtc = DateTime.UtcNow
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[0].Id,
                CreatedAtUtc = DateTime.UtcNow,
                InviteUrl = $"/matches/{match.Id}"
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[1].Id,
                CreatedAtUtc = DateTime.UtcNow,
                InviteUrl = $"/matches/{match.Id}"
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[2].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Content = "Hello! Excited for the match.",
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[0].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Location = new Location(0,0,"Porto", "Portugal")
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[1].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Location = new Location(0,0,"Porto", "Portugal")
            },
            new()
            {
                Id = $"chatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ConversationId = conversations[2].Id,
                CreatedAtUtc = DateTime.UtcNow,
                Location = new Location(0,0,"Porto", "Portugal")
            }
        };

        foreach (Conversation conversation in conversations)
        {
            conversation.LastMessageAtUtc = DateTime.UtcNow;
            conversation.LastMessageContent = "Hello! Excited for the match.";
        }

        db.ChatMessages.AddRange(messages);

        return db.SaveChangesAsync(ct);
    }
}
