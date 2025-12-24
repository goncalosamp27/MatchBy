using MatchBy.Enums;
namespace MatchBy.Models;

public class Friend
{
    public string Id { get; set; }
    
    public string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public FriendStatus Status { get; set; }
}