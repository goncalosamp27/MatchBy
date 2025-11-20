using FluentValidation.TestHelper;
using MatchBy.DTOs.PlayerRating;

namespace MatchBy.UnitTests.DTOs.PlayerRating;

public class UpdatePlayerRatingDtoValidatorTests
{
    private readonly UpdatePlayerRatingDtoValidator _validator = new();

    private UpdatePlayerRatingDto CreateValidDto()
    {
        return new UpdatePlayerRatingDto
        {
            Id = "playerrating_123",
            Rating = 4.5f,
            SentById = "sender_123"
        };
    }

    #region Id Validation Tests

    [Fact]
    public void Validate_IdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { Id = string.Empty };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id is required.");
    }

    [Fact]
    public void Validate_IdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { Id = new string('a', 501) };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Id cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_IdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Rating Validation Tests

    [Theory]
    [InlineData(-0.5f)]
    [InlineData(5.5f)]
    [InlineData(10f)]
    public void Validate_RatingOutOfRange_ShouldHaveValidationError(float rating)
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { Rating = rating };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("Rating must be between 0 and 5.");
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(2.5f)]
    [InlineData(5f)]
    public void Validate_RatingIsValid_ShouldNotHaveValidationError(float rating)
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { Rating = rating };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    #endregion

    #region SentById Validation Tests

    [Fact]
    public void Validate_SentByIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { SentById = string.Empty };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SentById)
              .WithErrorMessage("SentById is required.");
    }

    [Fact]
    public void Validate_SentByIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto() with { SentById = new string('a', 501) };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SentById)
              .WithErrorMessage("SentById cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_SentByIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SentById);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        UpdatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var dto = new UpdatePlayerRatingDto
        {
            Id = string.Empty,
            Rating = -1f,
            SentById = string.Empty
        };

        // Act
        TestValidationResult<UpdatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
        result.ShouldHaveValidationErrorFor(x => x.SentById);
    }

    #endregion
}

