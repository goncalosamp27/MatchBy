using FluentValidation.TestHelper;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Validators;

public class UpdateMatchDtoValidatorTests
{
    private readonly UpdateMatchDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { UserId = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId is required.");
    }

    [Fact]
    public void Validate_WhenUserIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { UserId = null! };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId is required.");
    }

    [Fact]
    public void Validate_WhenMatchIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MatchId = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
            .WithErrorMessage("MatchId is required.");
    }

    [Fact]
    public void Validate_WhenMatchIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MatchId = null! };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
            .WithErrorMessage("MatchId is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = null! };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionExceeds500Characters_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsExactly500Characters_ShouldPass()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Description = new string('a', 500) };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenMinPlayersIsZero_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MinPlayers = 0 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
            .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsGreaterThan10_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MinPlayers = 11 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
            .WithErrorMessage("Minimum players must be less than or equal to 10.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsZero_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MaxPlayers = 0 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsGreaterThan30_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MaxPlayers = 31 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsLessThanMinPlayers_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MinPlayers = 5, MaxPlayers = 3 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be greater than or equal to minimum players.");
    }

    [Fact]
    public void Validate_WhenLocationIsNull_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Location = null! };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location)
            .WithErrorMessage("Location is required.");
    }

    [Fact]
    public void Validate_WhenLocationLatitudeIsOutOfRange_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Location = new Location(91.0, 0, "City", "Country") };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
            .WithErrorMessage("Latitude must be between -90 and 90.");
    }

    [Fact]
    public void Validate_WhenLocationLongitudeIsOutOfRange_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Location = new Location(0, 181.0, "City", "Country") };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
            .WithErrorMessage("Longitude must be between -180 and 180.");
    }

    [Fact]
    public void Validate_WhenAddressIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Address = string.Empty };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_WhenAddressExceeds200Characters_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Address = new string('a', 201) };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WhenMatchDateTimeUtcIsInThePast_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1) };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchDateTimeUtc)
            .WithErrorMessage("Match date and time must be in the future.");
    }

    [Fact]
    public void Validate_WhenSportIsInvalidEnum_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Sport = (Sports)999 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sport)
            .WithErrorMessage("Invalid sport type.");
    }

    [Fact]
    public void Validate_WhenPrivacyIsInvalidEnum_ShouldFail()
    {
        // Arrange
        UpdateMatchDto dto = CreateValidDto() with { Privacy = (MatchPrivacy)999 };

        // Act
        TestValidationResult<UpdateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Privacy)
            .WithErrorMessage("Invalid privacy type.");
    }

    private static UpdateMatchDto CreateValidDto()
    {
        return new UpdateMatchDto
        {
            UserId = "user-id",
            MatchId = "match-id",
            Description = "Test match description",
            MinPlayers = 5,
            MaxPlayers = 10,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main Street",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public
        };
    }
}


