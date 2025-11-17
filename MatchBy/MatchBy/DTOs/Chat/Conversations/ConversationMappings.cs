using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Conversations;

public static class ConversationMappings
{
    public static ConversationDto ToDto(this Conversation conversation)
    {
        return new ConversationDto
        {
            Id = conversation.Id,
            Type = conversation.Type,
            Title = conversation.Title,
            ImageUrl = conversation.Image?.Url,
            CreatorId = conversation.CreatorId,
            TeamId = conversation.TeamId,
            MatchId = conversation.MatchId,
            CreatedAtUtc = conversation.CreatedAtUtc,
            UpdatedAtUtc = conversation.UpdatedAtUtc,
            LastMessageContent = conversation.LastMessageContent,
            MessagesCount = conversation.Messages?.Count ?? 0,
            LastMessageAtUtc = conversation.LastMessageAtUtc,
            Participants = [.. (conversation.Participants ?? Enumerable.Empty<ApplicationUser>()).Select(p => new ConversationParticipantDto
            {
                Id = p.Id,
                Username = p.UserName!, // assuming UserName is non-null because it's required in the user configuration
                ImageUrl = p.ProfileImage?.Url,
                DisplayName = p.DisplayName
            })]
        };
    }

    public static Conversation ToEntity(this CreateConversationDto dto)
    {
        return new Conversation
        {
            Id = $"conversation_{Guid.CreateVersion7()}",
            Type = dto.ConversationType,
            Title = dto.Title,
            CreatorId = dto.CreatorUserId,
            TeamId = dto.TeamId,
            MatchId = dto.MatchId,
            Image = null,
            Participants = [], // to be added later in the service layer
            Messages = [], // initially empty
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null,
            LastMessageAtUtc = null,
            DeletedAtUtc = null
        };
    }
}
