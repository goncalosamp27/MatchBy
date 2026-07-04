using FluentValidation;

namespace MatchBy.DTOs.Notification;

public class CreateNotificationDtoValidator : AbstractValidator<CreateNotificationDto>
{
    public CreateNotificationDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid notification type.");

        RuleFor(x => x.ReceiverUserId)
            .NotEmpty().WithMessage("Receiver user ID is required.")
            .MaximumLength(500).WithMessage("Receiver user ID cannot exceed 500 characters.");

        RuleFor(x => x.SenderUserId)
            .NotEmpty().WithMessage("Sender user ID is required.")
            .MaximumLength(500).WithMessage("Sender user ID cannot exceed 500 characters.");

        RuleFor(x => x.RelatedEntityId)
            .NotEmpty().WithMessage("Related entity ID is required.")
            .MaximumLength(500).WithMessage("Related entity ID cannot exceed 500 characters.");

        RuleFor(x => x.RelatedEntityName)
            .NotEmpty().WithMessage("Related entity name is required.")
            .MaximumLength(100).WithMessage("Related entity name cannot exceed 100 characters.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");

        RuleFor(x => x.ActionUrl)
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
            .WithMessage("Action URL must be a valid URL.")
            .MaximumLength(2048).WithMessage("Action URL cannot exceed 2048 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ActionUrl));
    }
}