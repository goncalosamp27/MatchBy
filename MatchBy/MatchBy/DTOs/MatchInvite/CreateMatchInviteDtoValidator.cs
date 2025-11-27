using FluentValidation;

namespace MatchBy.DTOs.MatchInvite;

public class CreateMatchInviteDtoValidator : AbstractValidator<CreateMatchInviteDto>
{
    public CreateMatchInviteDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters.");

        RuleFor(x => x.SenderId)
            .NotEmpty().WithMessage("SenderId is required.")
            .MaximumLength(500).WithMessage("SenderId cannot exceed 500 characters.");

        RuleFor(x => x.ReceiverId)
            .NotEmpty().WithMessage("ReceiverId is required.")
            .MaximumLength(500).WithMessage("ReceiverId cannot exceed 500 characters.");

        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("MatchId is required.")
            .MaximumLength(500).WithMessage("MatchId cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => x.SenderId != x.ReceiverId)
            .WithMessage("Sender and Receiver cannot be the same user.");

        When(x => x.ExpiresAtUtc.HasValue, () =>
        {
            RuleFor(x => x.ExpiresAtUtc!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Expiration date must be in the future.");
        });
    }
}



