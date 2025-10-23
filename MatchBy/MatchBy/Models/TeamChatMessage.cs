namespace MatchBy.Models;

public class TeamChatMessage: ChatMessage
{
    public ICollection<ApplicationUser> Receivers { get; set; }
    public string TeamId { get; set; }
    public Team? Team { get; set; }
}
