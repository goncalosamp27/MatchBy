using FluentValidation;
using MatchBy.Services.FileValidator;

namespace MatchBy.DTOs.Chat.Conversations;

public class UpdateConversationDtoValidator : AbstractValidator<UpdateConversationDto>
{
    public UpdateConversationDtoValidator(IFileValidator fileValidator)
    {
        double maxMb = fileValidator.GetMaxFileBytes() / (1024d * 1024d);
        
        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
        });
        
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("ConversationId is required.");
        
        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("CreatorUserId is required.");

        RuleFor(x => x.ParticipantIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Participants are required.")
            .Must(p => p.Count > 0).WithMessage("Provide at least one participant.")
            .Must(p => p.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("Some participant IDs are empty.")
            .Must(p => p.Distinct().Count() == p.Count)
            .WithMessage("Duplicate participant IDs are not allowed.");

        // Enforce that the creator is among participants
        RuleFor(x => x)
             .Must(x => x.ParticipantIds != null &&  x.ParticipantIds.Contains(x.CreatorUserId))
             .WithMessage("Creator must be included in ParticipantIds.");
        
        When(x => x.File is not null, () =>
        {
            RuleFor(x => x.File!)
                .Must(f => fileValidator.IsValidBrowserImage(f) || fileValidator.IsValidBrowserVideo(f))
                .WithMessage($"File is not allowed. Only .jpg, .jpeg, .png images or .mp4 videos are accepted, up to {maxMb:0.#} MB.");
        });
    }
}

