using MatchBy.Enums;

namespace MatchBy.DTOs.Notification;

public sealed record CreateNotificationDto
{
    public required NotificationType Type { get; init; }
    public required string ReceiverUserId { get; init; }
    public required string SenderUserId { get; init; }
    public required string RelatedEntityId { get; init; }
    public required string RelatedEntityName { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? ActionUrl { get; init; }
}





