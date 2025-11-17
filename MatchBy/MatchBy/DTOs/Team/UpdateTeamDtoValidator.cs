using FluentValidation;

namespace MatchBy.DTOs.Team;

public class UpdateTeamDtoValidator : AbstractValidator<UpdateTeamDto>
{
    public UpdateTeamDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("TeamId is required.");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(500).WithMessage("Name cannot exceed 500 characters.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        
        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("OwnerId is required.");
        
        RuleFor(x => x.Privacy)
            .IsInEnum().WithMessage("Privacy must be a valid value.");
        
        RuleFor(x => x.MembersIds)
            .NotNull().WithMessage("Members are required.")
            .Must(p => p.Count > 0).WithMessage("Provide at least one member.")
            .Must(p => p.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("Some member IDs are empty.")
            .Must(p => p.Distinct().Count() == p.Count)
            .WithMessage("Duplicate member IDs are not allowed.");

        // Enforce that the creator is among participants
        RuleFor(x => x)
            .Must(x => x.MembersIds.Contains(x.OwnerId))
            .WithMessage("Creator must be included in MembersIds.");
    }
}
