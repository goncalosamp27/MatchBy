using FluentValidation;
using MatchBy.Services.FileValidator;

namespace MatchBy.DTOs.Team;

public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamDtoValidator(IFileValidator fileValidator)
    {
        double maxMb = fileValidator.GetMaxFileBytes() / (1024d * 1024d);
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(500).WithMessage("Name cannot exceed 500 characters.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        
        RuleFor(x => x.MaxMembers)
            .GreaterThan(1).WithMessage("MaxMembers must be greater than 1.")
            .LessThanOrEqualTo(100).WithMessage("MaxMembers cannot exceed 100.");
        
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
        
        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!)
                .Must(fileValidator.IsValidBrowserImage)
                .WithMessage($"File is not allowed. Only .jpg, .jpeg, .png images are accepted, up to {maxMb:0.#} MB.");
        });
    }
}
