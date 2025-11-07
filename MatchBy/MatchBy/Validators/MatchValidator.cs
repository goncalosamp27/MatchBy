using FluentValidation;
using MatchBy.Models;

namespace MatchBy.Validators;

public class MatchValidator : AbstractValidator<Match>
{
    public MatchValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        
        RuleFor(x => x.minPlayers)
            .GreaterThan(0).WithMessage("Minimum players must be greater than 0.")
            .LessThanOrEqualTo(10).WithMessage("Minimum players must be less than or equal to 10.");
        
        RuleFor(x => x.maxPlayers)
            .GreaterThan(0).WithMessage("Maximum players must be greater than 0.")
            .LessThanOrEqualTo(10).WithMessage("Maximum players must be less than or equal to 30.")
            .GreaterThanOrEqualTo(x => x.minPlayers).WithMessage("Maximum players must be greater than or equal to minimum players.");
        
        RuleFor(x => x.Location)
            .NotNull().WithMessage("Location is required.");
        
        RuleFor(x => x.MatchDateTimeUtc)
            .NotNull().WithMessage("MatchDateTime is required.");
        
        RuleFor(x => x.Sport)
            .NotNull().WithMessage("Sport is required.");
        
        RuleFor(x => x.Status)
            .NotNull().WithMessage("Status is required.");
        
        RuleFor(x => x.Privacy)
            .NotNull().WithMessage("Privacy is required.");
        
        RuleFor(x => x.CreatorId)
            .NotNull().WithMessage("Creator is required.");

        RuleFor(x => x.CreatedAtUtc)
            .NotNull().WithMessage("CreatedAt is required.");
            
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("Address cannot exceed 100 characters.");
    }
}
