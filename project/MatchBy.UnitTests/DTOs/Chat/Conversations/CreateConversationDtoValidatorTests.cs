using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Chat.Conversations;

public class CreateConversationDtoValidatorTests
{
    private readonly CreateConversationDtoValidator _validator = new();

    [Fact]
    public void Validate_PrivateConversationWithTwoParticipants_ShouldPass()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TeamConversationWithTeamId_ShouldPass()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Team,
            ParticipantIds = new List<string> { "user_123", "user_456", "user_789" },
            Title = "Team Chat",
            TeamId = "team_123",
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MatchConversationWithMatchId_ShouldPass()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Match,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = "Match Chat",
            TeamId = null,
            MatchId = "match_123"
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_CreatorUserIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = string.Empty,
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_456", "user_789" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
              .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_ConversationTypeIsInvalid_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = (ConversationType)999,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConversationType)
              .WithErrorMessage("ConversationType is invalid.");
    }

    [Fact]
    public void Validate_ParticipantIdsIsEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string>(),
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
              .WithErrorMessage("Provide at least one participant.");
    }

    [Fact]
    public void Validate_ParticipantIdsContainsDuplicates_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_123", "user_123" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
              .WithErrorMessage("Duplicate participant IDs are not allowed.");
    }

    [Fact]
    public void Validate_CreatorNotInParticipants_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_456", "user_789" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Creator must be included in ParticipantIds.");
    }

    [Fact]
    public void Validate_PrivateConversationWithMoreThanTwoParticipants_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_123", "user_456", "user_789" },
            Title = null,
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
              .WithErrorMessage("Private conversations must have exactly two participants.");
    }

    [Fact]
    public void Validate_PrivateConversationWithTeamId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = null,
            TeamId = "team_123",
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
              .WithErrorMessage("Private conversations must not have a TeamId.");
    }

    [Fact]
    public void Validate_PrivateConversationWithMatchId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Private,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = null,
            TeamId = null,
            MatchId = "match_123"
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("Private conversations must not have a MatchId.");
    }

    [Fact]
    public void Validate_TeamConversationWithoutTeamId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Team,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = "Team Chat",
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
              .WithErrorMessage("TeamId is required for Team conversations.");
    }

    [Fact]
    public void Validate_TeamConversationWithMatchId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Team,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = "Team Chat",
            TeamId = "team_123",
            MatchId = "match_123"
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("Team conversations must not have a MatchId.");
    }

    [Fact]
    public void Validate_MatchConversationWithoutMatchId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Match,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = "Match Chat",
            TeamId = null,
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.MatchId)
              .WithErrorMessage("MatchId is required for Match conversations.");
    }

    [Fact]
    public void Validate_MatchConversationWithTeamId_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Match,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = "Match Chat",
            TeamId = "team_123",
            MatchId = "match_123"
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TeamId)
              .WithErrorMessage("Match conversations must not have a TeamId.");
    }

    [Fact]
    public void Validate_TitleExceedsMaxLength_ShouldHaveValidationError()
    {
        var dto = new CreateConversationDto
        {
            CreatorUserId = "user_123",
            ConversationType = ConversationType.Team,
            ParticipantIds = new List<string> { "user_123", "user_456" },
            Title = new string('a', 201),
            TeamId = "team_123",
            MatchId = null
        };
        TestValidationResult<CreateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title)
              .WithErrorMessage("Title cannot exceed 200 characters.");
    }
}

