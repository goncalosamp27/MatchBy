namespace MatchBy.Models;

public class PrivateChatMessage: ChatMessage
{
    public string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
}