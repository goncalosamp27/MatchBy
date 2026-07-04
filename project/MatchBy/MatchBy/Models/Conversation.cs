namespace MatchBy.Models;

public enum ConversationType
{
    Private,
    Team,
    Match
}

public class Conversation
{
    public string Id { get; set; }
    public ConversationType Type { get; set; }
    public string? Title { get; set; }
    public FileStore? Image { get; set; } 
    
    public string CreatorId { get; set; }
    public ApplicationUser? Creator { get; set; }
    
    public ICollection<ApplicationUser> Participants { get; set; }
    public ICollection<ChatMessage> Messages { get; set; }
    
    public string? TeamId { get; set; }
    public Team? Team { get; set; }
    
    public string? MatchId { get; set; }
    public Match? Match { get; set; }
    
    
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAtUtc { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
