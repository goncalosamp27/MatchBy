using FluentValidation.TestHelper;
using MatchBy.DTOs.TeamInvite;

namespace MatchBy.UnitTests.DTOs.TeamInvite;

public class CreateTeamInviteDtoValidatorTests
{
    private readonly CreateTeamInviteDtoValidator _validator = new();

    private CreateTeamInviteDto CreateValidDto()
    {
        return new CreateTeamInviteDto
        {
            Content = "Join our team!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            TeamId = "team_789",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };
    }

    [Fact]
    public void Validate_ContentIsEmpty_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { Content = string.Empty };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { Content = new string('a', 501) };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_SenderIdIsEmpty_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { SenderId = string.Empty };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.SenderId)
              .WithErrorMessage("SenderId is required.");
    }

    [Fact]
    public void Validate_ReceiverIdIsEmpty_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { ReceiverId = string.Empty };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ReceiverId)
              .WithErrorMessage("ReceiverId is required.");
    }

    [Fact]
    public void Validate_TeamIdIsEmpty_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { TeamId = string.Empty };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
              .WithErrorMessage("TeamId is required.");
    }

    [Fact]
    public void Validate_SenderAndReceiverAreSame_ShouldHaveValidationError()
    {
        var dto = new CreateTeamInviteDto
        {
            Content = "Join our team!",
            SenderId = "user_123",
            ReceiverId = "user_123",
            TeamId = "team_789",
            ExpiresAtUtc = null
        };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Sender and Receiver cannot be the same user.");
    }

    [Fact]
    public void Validate_ExpiresAtUtcInPast_ShouldHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAtUtc!.Value)
              .WithErrorMessage("Expiration date must be in the future.");
    }

    [Fact]
    public void Validate_ExpiresAtUtcIsNull_ShouldNotHaveValidationError()
    {
        CreateTeamInviteDto dto = CreateValidDto() with { ExpiresAtUtc = null };
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAtUtc);
    }

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        CreateTeamInviteDto dto = CreateValidDto();
        TestValidationResult<CreateTeamInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

