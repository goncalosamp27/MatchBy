using FluentValidation.TestHelper;
using MatchBy.DTOs.MatchInvite;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.MatchInvite;

public class UpdateMatchInviteDtoValidatorTests
{
    private readonly UpdateMatchInviteDtoValidator _validator = new();

    [Fact]
    public void Validate_IdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = string.Empty,
            Content = "Updated content",
            Status = null,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id is required.");
    }

    [Fact]
    public void Validate_ContentIsEmptyButNotNull_ShouldHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = string.Empty,
            Status = null,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content!)
              .WithErrorMessage("Content cannot be empty.");
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = new string('a', 1001),
            Status = null,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content!)
              .WithErrorMessage("Content cannot exceed 1000 characters.");
    }

    [Fact]
    public void Validate_ContentIsNull_ShouldNotHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = null,
            Status = null,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_StatusIsInvalidEnum_ShouldHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = null,
            Status = (InviteStatus)999,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Status!.Value)
              .WithErrorMessage("Status must be a valid InviteStatus value.");
    }

    [Fact]
    public void Validate_ExpiresAtUtcInPast_ShouldHaveValidationError()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = null,
            Status = null,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ExpiresAtUtc!.Value)
              .WithErrorMessage("Expiration date must be in the future.");
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_ShouldPass()
    {
        var dto = new UpdateMatchInviteDto
        {
            Id = "invite_123",
            Content = null,
            Status = null,
            ExpiresAtUtc = null
        };
        TestValidationResult<UpdateMatchInviteDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
        result.ShouldNotHaveValidationErrorFor(x => x.ExpiresAtUtc);
    }
}

