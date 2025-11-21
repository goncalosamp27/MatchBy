using FluentValidation.TestHelper;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Validators;

namespace MatchBy.UnitTests.Models;

public class MatchValidatorTests
{
    private readonly MatchValidator _validator = new();

    [Fact]
    public void Validate_WhenMatchIsValid_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDescriptionIsEmpty_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Description = string.Empty;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsNull_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Description = null!;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Validate_WhenDescriptionExceeds500Characters_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Description = new string('a', 501);

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenDescriptionIsExactly500Characters_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Description = new string('a', 500);

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_WhenMinPlayersIsZero_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = 0;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.minPlayers)
            .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsNegative_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = -1;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.minPlayers)
            .WithErrorMessage("Minimum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsGreaterThan30_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = 31;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.minPlayers)
            .WithErrorMessage("Minimum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_WhenMinPlayersIsExactly30_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = 30;
        match.maxPlayers = 30;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.minPlayers);
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsZero_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.maxPlayers = 0;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.maxPlayers)
            .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsNegative_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.maxPlayers = -1;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.maxPlayers)
            .WithErrorMessage("Maximum players must be greater than 0.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsGreaterThan30_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.maxPlayers = 31;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.maxPlayers)
            .WithErrorMessage("Maximum players must be less than or equal to 30.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsExactly30_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.maxPlayers = 30;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.maxPlayers);
    }

    [Fact]
    public void Validate_WhenMaxPlayersIsLessThanMinPlayers_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = 5;
        match.maxPlayers = 3;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.maxPlayers)
            .WithErrorMessage("Maximum players must be greater than or equal to minimum players.");
    }

    [Fact]
    public void Validate_WhenMaxPlayersEqualsMinPlayers_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.minPlayers = 5;
        match.maxPlayers = 5;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.maxPlayers);
    }

    [Fact]
    public void Validate_WhenLocationIsNull_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Location = null!;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Location)
            .WithErrorMessage("Location is required.");
    }
    
    [Fact]
    public void Validate_WhenCreatorIdIsNull_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.CreatorId = null!;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorId)
            .WithErrorMessage("Creator is required.");
    }
    
    [Fact]
    public void Validate_WhenAddressIsEmpty_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Address = string.Empty;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_WhenAddressIsNull_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Address = null!;

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address is required.");
    }

    [Fact]
    public void Validate_WhenAddressExceeds200Characters_ShouldFail()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Address = new string('a', 201);

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage("Address cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_WhenAddressIsExactly200Characters_ShouldPass()
    {
        // Arrange
        Match match = CreateValidMatch();
        match.Address = new string('a', 200);

        // Act
        TestValidationResult<Match> result = _validator.TestValidate(match);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Address);
    }

    private static Match CreateValidMatch()
    {
        return new Match
        {
            Id = "test-id",
            Description = "Test match description",
            minPlayers = 5,
            maxPlayers = 10,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            Address = "123 Main Street",
            MatchDateTimeUtc = DateTime.UtcNow.AddDays(1),
            Sport = Sports.Football,
            Status = MatchStatus.Pendent,
            Privacy = MatchPrivacy.Public,
            CreatorId = "creator-id",
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}


