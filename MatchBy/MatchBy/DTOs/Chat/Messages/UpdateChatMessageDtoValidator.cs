using FluentValidation;

namespace MatchBy.DTOs.Chat.Messages;

public class UpdateChatMessageDtoValidator : AbstractValidator<UpdateChatMessageDto>
{
    public UpdateChatMessageDtoValidator()
    {
        RuleFor(x => x.ChatMessageId)
            .NotEmpty().WithMessage("ChatMessageId is required.")
            .MaximumLength(500).WithMessage("ChatMessageId must not exceed 500 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Content cannot be whitespace only.")
            .MaximumLength(500).WithMessage("Content must not exceed 500 characters.");

        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("CreatorUserId is required.")
            .MaximumLength(500).WithMessage("CreatorUserId must not exceed 500 characters.");
    }
}
