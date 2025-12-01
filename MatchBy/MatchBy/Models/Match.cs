using MatchBy.DTOs.Match;
using MatchBy.Enums;

namespace MatchBy.Models;

public sealed class Match
{
    public string Id { get; set; }
    public Location Location { get; set; }
    public string Address { get; set; }
    public DateTime MatchDateTimeUtc { get; set; }
    public string Description { get; set; }
    public MinimumPlayersAverage MinimumPlayersRating { get; set; }
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public Sports Sport { get; set; }
    public MatchStatus Status { get; set; }
    public MatchPrivacy Privacy { get; set; }
    public string CreatorId { get; set; }
    public ApplicationUser? Creator { get; set; }
    public string? ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public ICollection<ApplicationUser> Participants { get; set; } = new List<ApplicationUser>();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; } 
    public bool Reminder3DaysSent { get; set; }
    public bool Reminder30MinSent { get; set; }
}