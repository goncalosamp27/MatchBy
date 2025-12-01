using FluentValidation;

namespace MatchBy.DTOs.PlayerRating;

public class UpdatePlayerRatingDtoValidator : AbstractValidator<UpdatePlayerRatingDto>
{
    public UpdatePlayerRatingDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.")
            .MaximumLength(500).WithMessage("Id cannot exceed 500 characters.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 5)
            .WithMessage("Rating must be between 0 and 5.");

        RuleFor(x => x.SentById)
            .NotEmpty().WithMessage("SentById is required.")
            .MaximumLength(500).WithMessage("SentById cannot exceed 500 characters.");
    }
}



