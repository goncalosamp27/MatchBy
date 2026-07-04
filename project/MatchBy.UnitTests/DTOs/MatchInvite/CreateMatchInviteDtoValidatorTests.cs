using FluentValidation.TestHelper;
using MatchBy.DTOs.MatchInvite;

namespace MatchBy.UnitTests.DTOs.MatchInvite;

public class CreateMatchInviteDtoValidatorTests
{
    private readonly CreateMatchInviteDtoValidator _validator = new();

    private static CreateMatchInviteDto CreateValidDto()
    {
        return new CreateMatchInviteDto
        {
            Content = "Join our match!",
            SenderId = "sender_123",
            ReceiverId = "receiver_456",
            MatchId = "match_789",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };
    }

    [Fact]
    public void Validate_ContentIsEmpty_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { Content = string.Empty };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { Content = new string('a', 1001) };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content cannot exceed 1000 characters.");
    }

    [Fact]
    public void Validate_SenderIdIsEmpty_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { SenderId = string.Empty };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.SenderId)
              .WithErrorMessage("SenderId is required.");
    }

    [Fact]
    public void Validate_ReceiverIdIsEmpty_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { ReceiverId = string.Empty };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ReceiverId)
              .WithErrorMessage("ReceiverId is required.");
    }

    [Fact]
    public void Validate_MatchIdIsEmpty_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { MatchId = string.Empty };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("MatchId is required.");
    }

    [Fact]
    public void Validate_SenderAndReceiverAreSame_ShouldHaveValidationError()
    {
        var dto = new CreateMatchInviteDto
        {
            Content = "Join our match!",
            SenderId = "user_123",
            ReceiverId = "user_123",
            MatchId = "match_789",
            ExpiresAtUtc = null
        };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Sender and Receiver cannot be the same user.");
    }

    [Fact]
    public void Validate_ExpiresAtUtcInPast_ShouldHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { ExpiresAtUtc = DateTime.UtcNow.AddDays(-1) };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAtUtc!.Value)
              .WithErrorMessage("Expiration date must be in the future.");
    }

    [Fact]
    public void Validate_ExpiresAtUtcIsNull_ShouldNotHaveValidationError()
    {
        CreateMatchInviteDto dto = CreateValidDto() with { ExpiresAtUtc = null };
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAtUtc);
    }

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        CreateMatchInviteDto dto = CreateValidDto();
        TestValidationResult<CreateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }
}

