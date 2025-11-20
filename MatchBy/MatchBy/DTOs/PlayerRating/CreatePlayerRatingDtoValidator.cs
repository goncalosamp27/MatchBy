using FluentValidation;
namespace MatchBy.DTOs.PlayerRating;

public class CreatePlayerRatingDtoValidator : AbstractValidator<CreatePlayerRatingDto>
{

    public CreatePlayerRatingDtoValidator()
    {
        RuleFor(x => x.SentById)
            .NotEmpty().WithMessage("Sender ID is required.");

        RuleFor(x => x.ReceivedById)
            .NotEmpty().WithMessage("Receiver ID is required.")
            .NotEqual(x => x.SentById).WithMessage("You cannot rate yourself.");

        RuleFor(x => x.MatchId)
            .NotEmpty().WithMessage("Match ID is required.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }


}
