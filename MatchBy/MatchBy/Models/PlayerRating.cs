namespace MatchBy.Models;

public class PlayerRating
{
    public string Id { get; set; }
    public float Rating { get; set; }
    public string SentById { get; set; }
    // public ApplicationUser? SentBy { get; set; }
    public string ReceivedById { get; set; }
    // public ApplicationUser? ReceivedBy { get; set; }
    public string MatchId { get; set; }
    // public Match? Match { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    // public DateTime? UpdatedAtUtc { get; set; }
    // public DateTime? DeletedAtUtc { get; set; }
}