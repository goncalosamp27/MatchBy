using FluentValidation;
using MatchBy.Models;

namespace MatchBy.DTOs.Chat.Conversations;

public class CreateConversationDtoValidator : AbstractValidator<CreateConversationDto>
{
    public CreateConversationDtoValidator()
    {
        RuleFor(x => x.CreatorUserId)
            .NotEmpty().WithMessage("CreatorUserId is required.");

        RuleFor(x => x.ConversationType)
            .IsInEnum().WithMessage("ConversationType is invalid.");

        RuleFor(x => x.ParticipantIds)
            .NotNull().WithMessage("Participants are required.")
            .Must(p => p.Count > 0).WithMessage("Provide at least one participant.")
            .Must(p => p.All(id => !string.IsNullOrWhiteSpace(id)))
            .WithMessage("Some participant IDs are empty.")
            .Must(p => p.Distinct().Count() == p.Count)
            .WithMessage("Duplicate participant IDs are not allowed.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        // Type-specific rules
        When(x => x.ConversationType == ConversationType.Private, () =>
        {
            RuleFor(x => x.TeamId).Empty().WithMessage("Private conversations must not have a TeamId.");
            RuleFor(x => x.MatchId).Empty().WithMessage("Private conversations must not have a MatchId.");
            RuleFor(x => x.ParticipantIds)
                .Must(p => p.Count == 2)
                .WithMessage("Private conversations must have exactly two participants.");
        });

        When(x => x.ConversationType == ConversationType.Team, () =>
        {
            RuleFor(x => x.TeamId).NotEmpty().WithMessage("TeamId is required for Team conversations.");
            RuleFor(x => x.MatchId).Empty().WithMessage("Team conversations must not have a MatchId.");
        });

        When(x => x.ConversationType == ConversationType.Match, () =>
        {
            RuleFor(x => x.MatchId).NotEmpty().WithMessage("MatchId is required for Match conversations.");
            RuleFor(x => x.TeamId).Empty().WithMessage("Match conversations must not have a TeamId.");
        });

        // Enforce that the creator is among participants
        RuleFor(x => x)
            .Must(x => x.ParticipantIds.Contains(x.CreatorUserId))
            .WithMessage("Creator must be included in ParticipantIds.");
    }
}
