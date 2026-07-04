using MatchBy.DTOs.User;

namespace MatchBy.DTOs.Notification;


public static class NotificationMappings
{
    public static NotificationDto ToDto(this Models.Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            Sender = notification.Sender?.ToDto(),
            Receiver = notification.Receiver?.ToDto(),
            SenderId = notification.SenderId,
            ReceiverId = notification.ReceiverId,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityName = notification.RelatedEntityName,
            IsRead = notification.IsRead,
            ActionUrl = notification.ActionUrl,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc,
        };
    }

    public static Models.Notification ToEntity(this CreateNotificationDto createDto)
    {
        return new Models.Notification
        {
            Id = $"notification_{Guid.CreateVersion7()}",
            Type = createDto.Type,
            Title = createDto.Title,
            Message = createDto.Message,
            SenderId = createDto.SenderUserId,
            ReceiverId = createDto.ReceiverUserId,
            RelatedEntityId = createDto.RelatedEntityId,
            RelatedEntityName = createDto.RelatedEntityName,
            CreatedAtUtc = DateTime.UtcNow,
            IsRead = false,
            ActionUrl = createDto.ActionUrl,
            ReadAtUtc = null,
        };
    }
}