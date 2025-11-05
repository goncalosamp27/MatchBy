using MatchBy.Enums;
namespace MatchBy.Models;

public sealed class Match
{
    public string Id { get; set; }
    public Location Location { get; set; }
    // public string Address { get; set; }
    public DateTime MatchDateTimeUtc { get; set; }
    public string Description { get; set; }
    public int minPlayers { get; set; }
    public int maxPlayers { get; set; }
    public Sports Sport { get; set; }
    public MatchStatus Status { get; set; }
    public MatchPrivacy Privacy { get; set; }
    public string CreatorId { get; set; }
    public ApplicationUser? Creator { get; set; }
    public string? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public ICollection<ApplicationUser> Participants { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
