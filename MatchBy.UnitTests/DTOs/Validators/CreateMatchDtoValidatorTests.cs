using FluentValidation.TestHelper;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Validators;

public class CreateMatchDtoValidatorTests
{
    private readonly CreateMatchDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto();

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDescriptionIsEmpty_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = null! };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = new string('a', 501) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsExactly500Characters_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Description = new string('a', 500) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenMinPlayersIsZero_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 0 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
            .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsNegative_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = -1 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
            .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsGreaterThan30_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 31 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MinPlayers)
            .WithErrorMessage("Minimum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsExactly30_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 30, MaxPlayers = 30 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MinPlayers);
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsZero_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = 0 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsNegative_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = -1 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsGreaterThan30_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = 31 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsExactly30_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MaxPlayers = 30 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxPlayers);
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsLessThanMinPlayers_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 5, MaxPlayers = 3 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MaxPlayers)
            .WithErrorMessage("Maximum players must be greater than or equal to minimum players.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersEqualsMinPlayers_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MinPlayers = 5, MaxPlayers = 5 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MaxPlayers);
    }

    [Fact]
    public void Validate_WhenLocationIsNull_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = null! };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location)
            .WithErrorMessage("Location is required.");
    }

    [Fact]
    public void Validate_WhenLocationLatitudeIsLessThanNegative90_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(-91.0, 0, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
            .WithErrorMessage("Latitude must be between -90 and 90.");
    }

    [Fact]
    public void Validate_WhenLocationLatitudeIsGreaterThan90_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(91.0, 0, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Latitude)
            .WithErrorMessage("Latitude must be between -90 and 90.");
    }

    [Fact]
    public void Validate_WhenLocationLongitudeIsLessThanNegative180_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(0, -181.0, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
            .WithErrorMessage("Longitude must be between -180 and 180.");
    }

    [Fact]
    public void Validate_WhenLocationLongitudeIsGreaterThan180_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Location = new Location(0, 181.0, "City", "Country") };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location.Longitude)
            .WithErrorMessage("Longitude must be between -180 and 180.");
    }

    [Fact]
    public void Validate_WhenAddressIsEmpty_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_WhenAddressIsNull_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = null! };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_WhenAddressExceeds200Characters_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = new string('a', 201) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WhenAddressIsExactly200Characters_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Address = new string('a', 200) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void Validate_WhenMatchDateTimeUtcIsDefault_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = default };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchDateTimeUtc)
            .WithErrorMessage("Match date and time is required.");
    }

    [Fact]
    public void Validate_WhenMatchDateTimeUtcIsInThePast_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddDays(-1) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchDateTimeUtc)
            .WithErrorMessage("Match date and time must be in the future.");
    }

    [Fact]
    public void Validate_WhenMatchDateTimeUtcIsInTheFuture_ShouldPass()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { MatchDateTimeUtc = DateTime.UtcNow.AddDays(1) };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MatchDateTimeUtc);
    }

    [Fact]
    public void Validate_WhenSportIsInvalidEnum_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Sport = (Sports)999 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sport)
            .WithErrorMessage("Invalid sport type.");
    }

    [Fact]
    public void Validate_WhenPrivacyIsInvalidEnum_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { Privacy = (MatchPrivacy)999 };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Privacy)
            .WithErrorMessage("Invalid privacy type.");
    }

    [Fact]
    public void Validate_WhenCreatorIdIsEmpty_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { CreatorId = string.Empty };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorId)
            .WithErrorMessage("Creator ID is required.");
    }

    [Fact]
    public void Validate_WhenCreatorIdIsNull_ShouldFail()
    {
        // Arrange
        CreateMatchDto dto = CreateValidDto() with { CreatorId = null! };

        // Act
        TestValidationResult<CreateMatchDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorId)
            .WithErrorMessage("Creator ID is required.");
    }

    private static CreateMatchDto CreateValidDto()
    {
        return new CreateMatchDto
        {
            Description = "Test match description",
            MinPlayers = 5,
            MaxPlayers = 10,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main Street",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Sport = Sports.Football,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator-id"
        };
    }
}


