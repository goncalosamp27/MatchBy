using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Validators;

public class CreateConversationDtoValidatorTests
{
    private readonly CreateConversationDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto();

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsEmpty_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { CreatorUserId = string.Empty };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsNull_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { CreatorUserId = null! };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenConversationTypeIsInvalidEnum_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ConversationType = (ConversationType)999 };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationType)
            .WithErrorMessage("ConversationType is invalid.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsIsNull_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = null! };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Participants are required.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsIsEmpty_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = new List<string>() };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Provide at least one participant.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsEmptyString_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = ["user1", string.Empty, "user2"] };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Some participant IDs are empty.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsWhitespace_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = new List<string> { "user1", "   ", "user2" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Some participant IDs are empty.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsDuplicates_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = new List<string> { "user1", "user2", "user1" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Duplicate participant IDs are not allowed.");
    }

    [Fact]
    public void Validate_WhenTitleExceeds200Characters_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { Title = new string('a', 201) };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WhenTitleIsExactly200Characters_ShouldPass()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { Title = new string('a', 200) };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenTitleIsNull_ShouldPass()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { Title = null };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenPrivateConversationHasTeamId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { TeamId = "team-id" };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
            .WithErrorMessage("Private conversations must not have a TeamId.");
    }

    [Fact]
    public void Validate_WhenPrivateConversationHasMatchId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { MatchId = "match-id" };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
            .WithErrorMessage("Private conversations must not have a MatchId.");
    }

    [Fact]
    public void Validate_WhenPrivateConversationHasMoreThanTwoParticipants_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = new List<string> { "user1", "user2", "user3" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Private conversations must have exactly two participants.");
    }

    [Fact]
    public void Validate_WhenPrivateConversationHasLessThanTwoParticipants_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { ParticipantIds = new List<string> { "user1" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Private conversations must have exactly two participants.");
    }

    [Fact]
    public void Validate_WhenTeamConversationHasNoTeamId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidTeamDto() with { TeamId = null };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
            .WithErrorMessage("TeamId is required for Team conversations.");
    }

    [Fact]
    public void Validate_WhenTeamConversationHasMatchId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidTeamDto() with { MatchId = "match-id" };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
            .WithErrorMessage("Team conversations must not have a MatchId.");
    }

    [Fact]
    public void Validate_WhenMatchConversationHasNoMatchId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidMatchDto() with { MatchId = null };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
            .WithErrorMessage("MatchId is required for Match conversations.");
    }

    [Fact]
    public void Validate_WhenMatchConversationHasTeamId_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidMatchDto() with { TeamId = "team-id" };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
            .WithErrorMessage("Match conversations must not have a TeamId.");
    }

    [Fact]
    public void Validate_WhenCreatorIsNotInParticipantIds_ShouldFail()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { CreatorUserId = "creator", ParticipantIds = new List<string> { "user1", "user2" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Creator must be included in ParticipantIds.");
    }

    [Fact]
    public void Validate_WhenCreatorIsInParticipantIds_ShouldPass()
    {
        // Arrange
        CreateConversationDto dto = CreateValidPrivateDto() with { CreatorUserId = "creator", ParticipantIds = new List<string> { "creator", "user2" } };

        // Act
        TestValidationResult<CreateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    private static CreateConversationDto CreateValidPrivateDto()
    {
        return new CreateConversationDto
        {
            CreatorUserId = "creator",
            ConversationType = ConversationType.Private,
            ParticipantIds = ["creator", "user2"],
            Title = "Test Conversation"
        };
    }

    private static CreateConversationDto CreateValidTeamDto()
    {
        return new CreateConversationDto
        {
            CreatorUserId = "creator",
            ConversationType = ConversationType.Team,
            ParticipantIds = ["creator", "user2", "user3"],
            TeamId = "team-id"
        };
    }

    private static CreateConversationDto CreateValidMatchDto()
    {
        return new CreateConversationDto
        {
            CreatorUserId = "creator",
            ConversationType = ConversationType.Match,
            ParticipantIds = ["creator", "user2", "user3"],
            MatchId = "match-id"
        };
    }
}


