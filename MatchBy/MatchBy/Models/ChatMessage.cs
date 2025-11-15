namespace MatchBy.Models;

public class ChatMessage
{
    public string Id { get; set; }
    public string? Content { get; set; }
    public string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public Location? Location { get; set; }
    public string? InviteUrl { get; set; }
    public string? ReplyToMessageId { get; set; }
    public ChatMessage? ReplyToMessage { get; set; }
    public string ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
