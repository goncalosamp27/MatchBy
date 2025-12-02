using FluentValidation;

namespace MatchBy.DTOs.PlayerRating;

public class CreatePlayerRatingDtoValidator : AbstractValidator<CreatePlayerRatingDto>
{
    public CreatePlayerRatingDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5)
            .WithMessage("Rating must be between 0 and 5.");

        RuleFor(x => x.SentById)
            .NotEmpty().WithMessage("SentById is required.")
            .MaximumLength(500).WithMessage("SentById cannot exceed 500 characters.");

        RuleFor(x => x.ReceivedById)
            .NotEmpty().WithMessage("ReceivedById is required.")
            .MaximumLength(500).WithMessage("ReceivedById cannot exceed 500 characters.");

        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("MatchId is required.")
            .MaximumLength(500).WithMessage("MatchId cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => x.SentById != x.ReceivedById)
            .WithMessage("Sender and Receiver cannot be the same user.");
    }
}



