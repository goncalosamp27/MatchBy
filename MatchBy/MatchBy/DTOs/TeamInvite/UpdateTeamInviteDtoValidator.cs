using FluentValidation;
using MatchBy.Models;

namespace MatchBy.DTOs.TeamInvite;

public class UpdateTeamInviteDtoValidator : AbstractValidator<UpdateTeamInviteDto>
{
    public UpdateTeamInviteDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.")
            .MaximumLength(500).WithMessage("Id cannot exceed 500 characters.");

        When(x => x.Content != null, () =>
        {
            RuleFor(x => x.Content!)
                .NotEmpty().WithMessage("Content cannot be empty.")
                .MaximumLength(500).WithMessage("Content cannot exceed 500 characters.");
        });

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status!.Value)
                .IsInEnum().WithMessage("Status must be a valid InviteStatus value.");
        });

        When(x => x.ExpiresAtUtc.HasValue, () =>
        {
            RuleFor(x => x.ExpiresAtUtc!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Expiration date must be in the future.");
        });
    }
}