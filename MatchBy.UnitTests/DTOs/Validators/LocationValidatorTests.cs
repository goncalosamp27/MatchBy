using FluentValidation.TestHelper;
using MatchBy.Models;
using MatchBy.Validators;

namespace MatchBy.UnitTests.DTOs.Validators;

public class LocationValidatorTests
{
    private readonly LocationValidator _validator = new();

    [Fact]
    public void Validate_WhenLocationIsValid_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenLatitudeIsLessThanNegative90_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(-91.0, location.Longitude, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Latitude)
            .WithErrorMessage("Latitude must be between -90 and 90 degrees.");
    }

    [Fact]
    public void Validate_WhenLatitudeIsGreaterThan90_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(91.0, location.Longitude, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Latitude)
            .WithErrorMessage("Latitude must be between -90 and 90 degrees.");
    }

    [Fact]
    public void Validate_WhenLatitudeIsExactlyNegative90_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(-90.0, location.Longitude, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Validate_WhenLatitudeIsExactly90_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(90.0, location.Longitude, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Validate_WhenLatitudeIsZero_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(0.0, location.Longitude, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Latitude);
    }

    [Fact]
    public void Validate_WhenLongitudeIsLessThanNegative180_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, -181.0, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Longitude)
            .WithErrorMessage("Longitude must be between -180 and 180 degrees.");
    }

    [Fact]
    public void Validate_WhenLongitudeIsGreaterThan180_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, 181.0, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Longitude)
            .WithErrorMessage("Longitude must be between -180 and 180 degrees.");
    }

    [Fact]
    public void Validate_WhenLongitudeIsExactlyNegative180_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, -180.0, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Validate_WhenLongitudeIsExactly180_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, 180.0, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Validate_WhenLongitudeIsZero_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, 0.0, location.City, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Longitude);
    }

    [Fact]
    public void Validate_WhenCityIsEmpty_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, location.Longitude, string.Empty, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City is required.");
    }

    [Fact]
    public void Validate_WhenCityIsNull_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, location.Longitude, null!, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City is required.");
    }

    [Fact]
    public void Validate_WhenCityExceeds100Characters_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        string longCity = new string('a', 101);
        location = new Location(location.Latitude, location.Longitude, longCity, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WhenCityIsExactly100Characters_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        string city = new string('a', 100);
        location = new Location(location.Latitude, location.Longitude, city, location.Country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.City);
    }

    [Fact]
    public void Validate_WhenCountryIsEmpty_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = location with { Country = string.Empty };

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country is required.");
    }

    [Fact]
    public void Validate_WhenCountryIsNull_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        location = new Location(location.Latitude, location.Longitude, location.City, null!);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country is required.");
    }

    [Fact]
    public void Validate_WhenCountryExceeds100Characters_ShouldFail()
    {
        // Arrange
        Location location = CreateValidLocation();
        string longCountry = new string('a', 101);
        location = new Location(location.Latitude, location.Longitude, location.City, longCountry);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WhenCountryIsExactly100Characters_ShouldPass()
    {
        // Arrange
        Location location = CreateValidLocation();
        string country = new string('a', 100);
        location = new Location(location.Latitude, location.Longitude, location.City, country);

        // Act
        TestValidationResult<Location> result = _validator.TestValidate(location);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Country);
    }

    private static Location CreateValidLocation()
    {
        return new Location(40.7128, -74.0060, "New York", "USA");
    }
}


