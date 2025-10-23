namespace MatchBy.Models;

public class MatchInvite: Invite
{
    public string MatchId { get; set; }
    public Match? Match { get; set; }
}