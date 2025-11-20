using FluentValidation.TestHelper;
using MatchBy.DTOs.Friend;

namespace MatchBy.UnitTests.DTOs.Friend;

public class UpdateFriendDtoValidatorTests
{
    private readonly UpdateFriendDtoValidator _validator = new();

    [Fact]
    public void Validate_IdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateFriendDto
        {
            Id = string.Empty
        };

        // Act
        TestValidationResult<UpdateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id is required.");
    }

    [Fact]
    public void Validate_IdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new UpdateFriendDto
        {
            Id = new string('a', 501)
        };

        // Act
        TestValidationResult<UpdateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_IdIsValid_ShouldPass()
    {
        // Arrange
        var dto = new UpdateFriendDto
        {
            Id = "friend_123"
        };

        // Act
        TestValidationResult<UpdateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

