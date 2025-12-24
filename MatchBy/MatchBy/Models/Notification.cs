using MatchBy.Enums;

namespace MatchBy.Models;

public class Notification
{
    public required string Id { get; set; }
    public required NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string SenderId { get; set; }
    public ApplicationUser? Sender { get; set; }
    public required string ReceiverId { get; set; }
    public ApplicationUser? Receiver { get; set; }
    public required string RelatedEntityId { get; set; }
    public required string RelatedEntityName { get; set; }
    public required bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}