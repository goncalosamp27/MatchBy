using FluentValidation;

namespace MatchBy.DTOs.Friend;

public class CreateFriendDtoValidator : AbstractValidator<CreateFriendDto>
{
    public CreateFriendDtoValidator()
    {
        RuleFor(x => x.SenderId)
            .NotEmpty().WithMessage("SenderId is required.")
            .MaximumLength(500).WithMessage("SenderId cannot exceed 500 characters.");

        RuleFor(x => x.ReceiverId)
            .NotEmpty().WithMessage("ReceiverId is required.")
            .MaximumLength(500).WithMessage("ReceiverId cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => x.SenderId != x.ReceiverId)
            .WithMessage("Sender and Receiver cannot be the same user.");
    }
}
