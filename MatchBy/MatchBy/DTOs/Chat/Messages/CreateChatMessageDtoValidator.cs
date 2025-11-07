using FluentValidation;

namespace MatchBy.DTOs.Chat.Messages;

public class CreateChatMessageDtoValidator : AbstractValidator<CreateChatMessageDto>
{
    public CreateChatMessageDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Content cannot be whitespace only.")
            .MaximumLength(500).WithMessage("Content must not exceed 500 characters.");

        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("CreatorUserId is required.")
            .MaximumLength(500).WithMessage("CreatorUserId must not exceed 500 characters.");

        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required.")
            .MaximumLength(500).WithMessage("ConversationId must not exceed 500 characters.");

        RuleFor(x => x.ReplyToMessageId)
            .MaximumLength(500).WithMessage("ReplyToMessageId must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReplyToMessageId));
    }
}
