namespace MatchBy.Models;

public class MatchChatMessage: ChatMessage
{
    public ICollection<ApplicationUser> Receivers { get; set; }
    public string MatchId { get; set; }
    public Match? Match { get; set; }
}