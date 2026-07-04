using FluentValidation.TestHelper;
using MatchBy.DTOs.Friend;

namespace MatchBy.UnitTests.DTOs.Friend;

public class CreateFriendDtoValidatorTests
{
    private readonly CreateFriendDtoValidator _validator = new();

    private static CreateFriendDto CreateValidDto()
    {
        return new CreateFriendDto
        {
            SenderId = "sender_123",
            ReceiverId = "receiver_456"
        };
    }

    #region SenderId Validation Tests

    [Fact]
    public void Validate_SenderIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto() with { SenderId = string.Empty };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SenderId)
              .WithErrorMessage("SenderId is required.");
    }

    [Fact]
    public void Validate_SenderIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto() with { SenderId = new string('a', 501) };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SenderId)
              .WithErrorMessage("SenderId cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_SenderIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SenderId);
    }

    #endregion

    #region ReceiverId Validation Tests

    [Fact]
    public void Validate_ReceiverIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto() with { ReceiverId = string.Empty };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceiverId)
              .WithErrorMessage("ReceiverId is required.");
    }

    [Fact]
    public void Validate_ReceiverIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto() with { ReceiverId = new string('a', 501) };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceiverId)
              .WithErrorMessage("ReceiverId cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_ReceiverIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReceiverId);
    }

    #endregion

    #region Sender/Receiver Equality Validation Tests

    [Fact]
    public void Validate_SenderAndReceiverAreSame_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreateFriendDto
        {
            SenderId = "user_123",
            ReceiverId = "user_123"
        };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Sender and Receiver cannot be the same user.");
    }

    [Fact]
    public void Validate_SenderAndReceiverAreDifferent_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        CreateFriendDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var dto = new CreateFriendDto
        {
            SenderId = string.Empty,
            ReceiverId = string.Empty
        };

        // Act
        TestValidationResult<CreateFriendDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SenderId);
        result.ShouldHaveValidationErrorFor(x => x.ReceiverId);
    }

    #endregion
}

