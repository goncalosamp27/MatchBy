namespace MatchBy.Models;

public class Team
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string OwnerId { get; set; }
    public TeamPrivacy Privacy { get; set; }
    public ApplicationUser? Owner { get; set; }
    public ICollection<ApplicationUser> Members { get; set; }
    public string? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}

public enum TeamPrivacy
{
    Public,
    Private
}
