using FluentValidation;

namespace MatchBy.DTOs.Chat.Messages;

public class CreateChatMessageDtoValidator : AbstractValidator<CreateChatMessageDto>
{
    public CreateChatMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .When(x => string.IsNullOrWhiteSpace(x.InviteUrl) && x.Location == null);

        RuleFor(x => x.Content)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Content cannot be whitespace only.")
            .When(x => !string.IsNullOrWhiteSpace(x.Content));

        RuleFor(x => x.Content)
            .MaximumLength(500).WithMessage("Content must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Content));

        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("CreatorUserId is required.")
            .MaximumLength(500).WithMessage("CreatorUserId must not exceed 500 characters.");

        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required.")
            .MaximumLength(500).WithMessage("ConversationId must not exceed 500 characters.");

        RuleFor(x => x.ReplyToMessageId)
            .MaximumLength(500).WithMessage("ReplyToMessageId must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReplyToMessageId));
        
        RuleFor(x => x.InviteUrl)
            .MaximumLength(500).WithMessage("InviteUrl must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.InviteUrl));

        RuleFor(x => x)
            .Must(x => !HasInviteUrlWithOtherFields(x))
            .WithMessage("A message with InviteUrl cannot contain Content or Location.")
            .Must(x => !HasLocationWithOtherFields(x))
            .WithMessage("A message with Location cannot contain Content or InviteUrl.")
            .Must(x => !HasContentWithOtherFields(x))
            .WithMessage("A message with Content cannot contain InviteUrl or Location.")
            .Must(HasAtLeastOneField)
            .WithMessage("A message must have either Content, InviteUrl, or Location.");
    }

    private static bool HasInviteUrlWithOtherFields(CreateChatMessageDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.InviteUrl) && 
               (!string.IsNullOrWhiteSpace(dto.Content) || dto.Location != null);
    }

    private static bool HasLocationWithOtherFields(CreateChatMessageDto dto)
    {
        return dto.Location != null && 
               (!string.IsNullOrWhiteSpace(dto.Content) || !string.IsNullOrWhiteSpace(dto.InviteUrl));
    }

    private static bool HasContentWithOtherFields(CreateChatMessageDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Content) && 
               (dto.Location != null || !string.IsNullOrWhiteSpace(dto.InviteUrl));
    }

    private static bool HasAtLeastOneField(CreateChatMessageDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.Content) || 
               dto.Location != null || 
               !string.IsNullOrWhiteSpace(dto.InviteUrl);
    }
}
