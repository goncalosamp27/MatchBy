namespace MatchBy.Models;

public class TeamInvite: Invite
{
    public string TeamId { get; set; }
    public Team? Team { get; set; }
}