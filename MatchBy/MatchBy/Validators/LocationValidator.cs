using FluentValidation;
using MatchBy.Models;

namespace MatchBy.Validators;

public class LocationValidator : AbstractValidator<Location>
{
    public LocationValidator()
    {
        RuleFor(l => l.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90 degrees.");
        
        RuleFor(l => l.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180 degrees.");
        
        RuleFor(l => l.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");
        
        RuleFor(l => l.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");
    }
}
