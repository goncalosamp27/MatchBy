using FluentValidation;

namespace MatchBy.DTOs.Friend;

public class UpdateFriendDtoValidator : AbstractValidator<UpdateFriendDto>
{
    public UpdateFriendDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.")
            .MaximumLength(500).WithMessage("Id cannot exceed 500 characters.");
    }
}










