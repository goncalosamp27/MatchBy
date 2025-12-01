namespace MatchBy.Models;

public abstract class Invite
{ 
    public string Id { get; set; }
    public string Content { get; set; }
    public string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    
    public InviteStatus Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; } 
    public DateTime? DeletedAtUtc { get; set; }
}

public enum InviteStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 3,
    Deleted = 4
}
