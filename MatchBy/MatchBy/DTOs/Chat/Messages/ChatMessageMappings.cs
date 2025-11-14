using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Messages;

public static class ChatMessageMappings
{
   public static ChatMessageDto ToDto(this ChatMessage chatMessage)
    {
        if (chatMessage.Sender is null)
        {
            throw new InvalidOperationException("Cannot map ChatMessage to ChatMessageDto when Sender is null.");
        }
        
        return new ChatMessageDto
        {
            Id = chatMessage.Id,
            Content = chatMessage.Content,
            SenderId = chatMessage.SenderId,
            Sender = chatMessage.Sender.ToUserSummaryDto(),
            Location = chatMessage.Location,
            ReplyToMessageId = chatMessage.ReplyToMessageId,
            ReplyToMessage = chatMessage.ReplyToMessage?.ToDto(),
            ConversationId = chatMessage.ConversationId,
            CreatedAtUtc = chatMessage.CreatedAtUtc,
            UpdatedAtUtc = chatMessage.UpdatedAtUtc,
            DeletedAtUtc = chatMessage.DeletedAtUtc
        };
    }

    public static ChatMessage ToEntity(this CreateChatMessageDto createChatMessageDto)
    {
        return new ChatMessage
        {
            Id = $"chatMessage_{Guid.CreateVersion7()}",
            Content = createChatMessageDto.Content,
            SenderId = createChatMessageDto.CreatorUserId,
            ReplyToMessageId = createChatMessageDto.ReplyToMessageId,
            ConversationId = createChatMessageDto.ConversationId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            DeletedAtUtc = null
        };
    }

    private static UserSummaryDto ToUserSummaryDto(this ApplicationUser user)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            DisplayName = user.UserName!, // assuming UserName is non-null because it's required in the user configuration
            AvatarUrl = user.ProfileImage?.Url
        };
    }
}
