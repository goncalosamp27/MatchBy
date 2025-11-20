using FluentValidation;
namespace MatchBy.DTOs.Match;

public class CreateMatchDtoValidator : AbstractValidator<CreateMatchDto>
{
    public CreateMatchDtoValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.MinPlayers)
            .GreaterThan(0).WithMessage("Minimum players must be greater than 0.")
            .LessThanOrEqualTo(30).WithMessage("Minimum players must be less than or equal to 30.");

        RuleFor(x => x.MaxPlayers)
            .GreaterThan(0).WithMessage("Maximum players must be greater than 0.")
            .LessThanOrEqualTo(30).WithMessage("Maximum players must be less than or equal to 30.")
            .GreaterThanOrEqualTo(x => x.MinPlayers)
            .WithMessage("Maximum players must be greater than or equal to minimum players.");

        RuleFor(x => x.Location)
            .NotNull().WithMessage("Location is required.");

        RuleFor(x => x.Location.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Location != null);

        RuleFor(x => x.Location.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Location != null);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("Address cannot exceed 200 characters.");

        RuleFor(x => x.MatchDateTimeUtc)
            .NotEmpty().WithMessage("Match date and time is required.")
            .GreaterThan(DateTime.UtcNow).WithMessage("Match date and time must be in the future.");

        RuleFor(x => x.Sport)
            .IsInEnum().WithMessage("Invalid sport type.");

        RuleFor(x => x.Privacy)
            .IsInEnum().WithMessage("Invalid privacy type.");

        RuleFor(x => x.CreatorId)
            .NotEmpty().WithMessage("Creator ID is required.");
    }
}
