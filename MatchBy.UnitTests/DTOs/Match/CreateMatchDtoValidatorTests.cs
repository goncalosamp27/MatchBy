using FluentValidation.TestHelper;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Match;

public class CreateMatchDtoValidatorTests
{
    private readonly CreateMatchDtoValidator _validator = new();

    private CreateMatchDto CreateValidDto()
    {
        return new CreateMatchDto
        {
            Location = new Location(40.7128,
                -74.0060,
                "New York",
                "USA"),
            Address = "123 Main St, New York, NY",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(7),
            Description = "Friendly football match",
            MinPlayers = 4,
            MaxPlayers = 10,
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator_123",
            MinimumPlayersRating = MinimumPlayersAverage.All
        };
    }

    #region Description Validation Tests

    [Fact]
    public void Validate_DescriptionIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_DescriptionIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    #endregion

    #region MinPlayers Validation Tests

    [Fact]
    public void Validate_MinPlayersIsZero_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 0 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
              .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_MinPlayersExceeds30_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 31 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
              .WithErrorMessage("Minimum players must be less than or equal to 30.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Validate_MinPlayersIsValid_ShouldNotHaveValidationError(int minPlayers)
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = minPlayers, MaxPlayers = minPlayers + 5 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinPlayers);
    }

    #endregion

    #region MaxPlayers Validation Tests

    [Fact]
    public void Validate_MaxPlayersIsZero_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = 0 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
              .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_MaxPlayersExceeds30_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = 31 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
              .WithErrorMessage("Maximum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_MaxPlayersLessThanMinPlayers_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 10, MaxPlayers = 5 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
              .WithErrorMessage("Maximum players must be greater than or equal to minimum players.");
    }

    [Theory]
    [InlineData(5, 10)]
    [InlineData(2, 2)]
    [InlineData(10, 30)]
    public void Validate_MaxPlayersIsValid_ShouldNotHaveValidationError(int minPlayers, int maxPlayers)
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = minPlayers, MaxPlayers = maxPlayers };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxPlayers);
    }

    #endregion

    #region Location Validation Tests
    
    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_LatitudeOutOfRange_ShouldHaveValidationError(double latitude)
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(latitude, 0, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

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
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(0, longitude, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
              .WithErrorMessage("Longitude must be between -180 and 180.");
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    [InlineData(40.7128, -74.0060)]
    public void Validate_LocationIsValid_ShouldNotHaveValidationError(double latitude, double longitude)
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(latitude, longitude, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Location.Latitude);
        result.ShouldNotHaveValidationErrorFor(x => x.Location.Longitude);
    }

    #endregion

    #region Address Validation Tests

    [Fact]
    public void Validate_AddressIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_AddressExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = new string('a', 201) };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
              .WithErrorMessage("Address cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_AddressIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    #endregion

    #region MatchDateTime Validation Tests

    [Fact]
    public void Validate_MatchDateTimeInPast_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1) };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchDateTimeUtc)
              .WithErrorMessage("Match date and time must be in the future.");
    }

    [Fact]
    public void Validate_MatchDateTimeInFuture_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddHours(1) };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MatchDateTimeUtc);
    }

    #endregion

    #region Sport Validation Tests

    [Fact]
    public void Validate_SportIsValidEnum_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto1 = CreateValidDto() with { Sport = Sports.Football };
        CreateMatchDto dto2 = CreateValidDto() with { Sport = Sports.Basketball };
        CreateMatchDto dto3 = CreateValidDto() with { Sport = Sports.Tennis };

        // Act
        TestValidationResult<CreateMatchDto>? result1 = _validator.TestValidate(dto1);
        TestValidationResult<CreateMatchDto>? result2 = _validator.TestValidate(dto2);
        TestValidationResult<CreateMatchDto>? result3 = _validator.TestValidate(dto3);

        // Assert
        result1.ShouldNotHaveValidationErrorFor(x => x.Sport);
        result2.ShouldNotHaveValidationErrorFor(x => x.Sport);
        result3.ShouldNotHaveValidationErrorFor(x => x.Sport);
    }

    [Fact]
    public void Validate_SportIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Sport = (Sports)999 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sport)
              .WithErrorMessage("Invalid sport type.");
    }

    #endregion

    #region Privacy Validation Tests

    [Fact]
    public void Validate_PrivacyIsValidEnum_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto1 = CreateValidDto() with { Privacy = MatchPrivacy.Public };
        CreateMatchDto dto2 = CreateValidDto() with { Privacy = MatchPrivacy.Private };

        // Act
        TestValidationResult<CreateMatchDto>? result1 = _validator.TestValidate(dto1);
        TestValidationResult<CreateMatchDto>? result2 = _validator.TestValidate(dto2);

        // Assert
        result1.ShouldNotHaveValidationErrorFor(x => x.Privacy);
        result2.ShouldNotHaveValidationErrorFor(x => x.Privacy);
    }

    [Fact]
    public void Validate_PrivacyIsInvalidEnum_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Privacy = (MatchPrivacy)999 };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Privacy)
              .WithErrorMessage("Invalid privacy type.");
    }

    #endregion

    #region CreatorId Validation Tests

    [Fact]
    public void Validate_CreatorIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { CreatorId = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorId)
              .WithErrorMessage("Creator ID is required.");
    }

    [Fact]
    public void Validate_CreatorIdIsValid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatorId);
    }

    #endregion

    #region Complete DTO Validation Tests

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateMatchDto>? result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}

