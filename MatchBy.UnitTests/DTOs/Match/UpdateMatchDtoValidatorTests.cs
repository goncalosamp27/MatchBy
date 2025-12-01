using FluentValidation.TestHelper;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Match;

public class UpdateMatchDtoValidatorTests
{
    private readonly UpdateMatchDtoValidator _validator = new();

    private UpdateMatchDto CreateValidDto()
    {
        return new UpdateMatchDto
        {
            UserId = "user_123",
            MatchId = "match_456",
            Location = new Location(40.7128,
                -74.0060,
                "New York",
                "USA"),
            Address = "123 Main St, New York, NY",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(7),
            Description = "Updated football match",
            MinPlayers = 4,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            MinimumPlayersRating = MinimumPlayersAverage.All
        };
    }

    #region UserId Validation Tests

    [Fact]
    public void Validate_UserIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { UserId = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
              .WithErrorMessage("UserId is required.");
    }

    [Fact]
    public void Validate_UserIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region MatchId Validation Tests

    [Fact]
    public void Validate_MatchIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MatchId = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("MatchId is required.");
    }

    [Fact]
    public void Validate_MatchIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MatchId);
    }

    #endregion

    #region Description Validation Tests

    [Fact]
    public void Validate_DescriptionIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    #endregion

    #region MinPlayers and MaxPlayers Validation Tests

    [Fact]
    public void Validate_MinPlayersIsZero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MinPlayers = 0 };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
              .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_MaxPlayersLessThanMinPlayers_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MinPlayers = 10, MaxPlayers = 5 };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
              .WithErrorMessage("Maximum players must be greater than or equal to minimum players.");
    }

    #endregion

    #region Location Validation Tests

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_LatitudeOutOfRange_ShouldHaveValidationError(double latitude)
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Location = new Location(latitude, 0, "City", "Country") };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
              .WithErrorMessage("Latitude must be between -90 and 90.");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_LongitudeOutOfRange_ShouldHaveValidationError(double longitude)
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Location = new Location(0, longitude, "City", "Country") };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
              .WithErrorMessage("Longitude must be between -180 and 180.");
    }

    #endregion

    #region Address Validation Tests

    [Fact]
    public void Validate_AddressIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Address = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_AddressExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Address = new string('a', 201) };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address cannot exceed 200 characters.");
    }

    #endregion

    #region MatchDateTime Validation Tests

    [Fact]
    public void Validate_MatchDateTimeInPast_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1) };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchDateTimeUtc)
              .WithErrorMessage("Match date and time must be in the future.");
    }

    #endregion

    #region Sport and Privacy Validation Tests

    [Fact]
    public void Validate_SportIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Sport = (Sports)999 };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sport)
              .WithErrorMessage("Invalid sport type.");
    }

    [Fact]
    public void Validate_PrivacyIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Privacy = (MatchPrivacy)999 };

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Privacy)
              .WithErrorMessage("Invalid privacy type.");
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

