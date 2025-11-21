using MatchBy.DTOs.User;
using MatchBy.Enums;

namespace MatchBy.DTOs.Notification;

public sealed record NotificationDto
{
    public required string Id { get; init; }
    public required NotificationType Type { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string SenderId { get; init; }
    public UserDto? Sender { get; init; }
    public required string ReceiverId { get; init; }
    public UserDto? Receiver { get; init; }
    public required string RelatedEntityId { get; init; } // TeamId, MatchId, ConversationId, etc
    public required string RelatedEntityName { get; init; } // Team name, Match name, etc
    public bool IsRead { get; init; }
    public string? ActionUrl { get; init; } // URL to navigate when clicking the notification
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
}


