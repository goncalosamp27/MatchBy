using FluentValidation.TestHelper;
using MatchBy.DTOs.PlayerRating;

namespace MatchBy.UnitTests.DTOs.PlayerRating;

public class CreatePlayerRatingDtoValidatorTests
{
    private readonly CreatePlayerRatingDtoValidator _validator = new();

    private CreatePlayerRatingDto CreateValidDto()
    {
        return new CreatePlayerRatingDto
        {
            Rating = 4,
            SentById = "sender_123",
            ReceivedById = "receiver_456",
            MatchId = "match_789"
        };
    }

    #region Rating Validation Tests

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_RatingOutOfRange_ShouldHaveValidationError(int rating)
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { Rating = rating };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rating)
              .WithErrorMessage("Rating must be between 0 and 5.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void Validate_RatingIsValid_ShouldNotHaveValidationError(int rating)
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { Rating = rating };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    #endregion

    #region SentById Validation Tests

    [Fact]
    public void Validate_SentByIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { SentById = string.Empty };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SentById)
              .WithErrorMessage("SentById is required.");
    }

    [Fact]
    public void Validate_SentByIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { SentById = new string('a', 501) };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SentById)
              .WithErrorMessage("SentById cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_SentByIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SentById);
    }

    #endregion

    #region ReceivedById Validation Tests

    [Fact]
    public void Validate_ReceivedByIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { ReceivedById = string.Empty };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceivedById)
              .WithErrorMessage("ReceivedById is required.");
    }

    [Fact]
    public void Validate_ReceivedByIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { ReceivedById = new string('a', 501) };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReceivedById)
              .WithErrorMessage("ReceivedById cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_ReceivedByIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReceivedById);
    }

    #endregion

    #region MatchId Validation Tests

    [Fact]
    public void Validate_MatchIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { MatchId = string.Empty };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("MatchId is required.");
    }

    [Fact]
    public void Validate_MatchIdExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto() with { MatchId = new string('a', 501) };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("MatchId cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_MatchIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MatchId);
    }

    #endregion

    #region Sender/Receiver Equality Validation Tests

    [Fact]
    public void Validate_SenderAndReceiverAreSame_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreatePlayerRatingDto
        {
            Rating = 4,
            SentById = "user_123",
            ReceivedById = "user_123",
            MatchId = "match_789"
        };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Sender and Receiver cannot be the same user.");
    }

    [Fact]
    public void Validate_SenderAndReceiverAreDifferent_ShouldNotHaveValidationError()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        CreatePlayerRatingDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllFieldsInvalid_ShouldHaveMultipleErrors()
    {
        // Arrange
        var dto = new CreatePlayerRatingDto
        {
            Rating = -1,
            SentById = string.Empty,
            ReceivedById = string.Empty,
            MatchId = string.Empty
        };

        // Act
        TestValidationResult<CreatePlayerRatingDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rating);
        result.ShouldHaveValidationErrorFor(x => x.SentById);
        result.ShouldHaveValidationErrorFor(x => x.ReceivedById);
        result.ShouldHaveValidationErrorFor(x => x.MatchId);
    }

    #endregion
}

