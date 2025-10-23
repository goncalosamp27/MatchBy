using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Data.Seeders;

public class PrivateChatMessageSeeder: ISeeder
{
    public Task SeedAsync(ApplicationDbContext db, IServiceProvider sp, CancellationToken ct)
    {
        if (db.PrivateChatMessages.Any())
        {
            return Task.CompletedTask;
        }

        var users = db.Users.ToList();
        if (users.Count < 3)
        {
            return Task.CompletedTask;
        }
        
        db.PrivateChatMessages.Add(
            new PrivateChatMessage
            {
                Id = $"privateChatMessage_{Guid.CreateVersion7()}",
                SenderId = users[1].Id,
                ReceiverId = users[2].Id,
                Content = "Looking forward to the match!",
                Status = MessageStatus.Available,
                CreatedAtUtc = DateTime.UtcNow
            });
        
        return db.SaveChangesAsync(ct);
    }
}
