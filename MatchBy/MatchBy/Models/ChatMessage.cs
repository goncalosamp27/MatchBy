using MatchBy.Enums;
namespace MatchBy.Models;

public abstract class ChatMessage
{
    public string Id { get; set; }
    public string Content { get; set; }
    public MessageStatus Status { get; set; }
    public string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
