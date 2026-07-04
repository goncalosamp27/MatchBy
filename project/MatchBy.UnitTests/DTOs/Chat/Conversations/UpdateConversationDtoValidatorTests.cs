using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Services.FileValidator;
using Moq;

namespace MatchBy.UnitTests.DTOs.Chat.Conversations;

public class UpdateConversationDtoValidatorTests
{
    private readonly Mock<IFileValidator> _fileValidatorMock = new();
    private readonly UpdateConversationDtoValidator _validator;

    public UpdateConversationDtoValidatorTests()
    {
        _fileValidatorMock.Setup(x => x.GetMaxFileBytes()).Returns(5 * 1024 * 1024); // 5 MB
        _validator = new UpdateConversationDtoValidator(_fileValidatorMock.Object);
    }

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TitleIsNull_ShouldNotHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = null,
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_TitleExceedsMaxLength_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = new string('a', 201),
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Title!)
              .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_ConversationIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = string.Empty,
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result;
        result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
              .WithErrorMessage("ConversationId is required.");
    }

    [Fact]
    public void Validate_CreatorUserIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = string.Empty,
            ParticipantIds = new List<string> { "user_456" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
              .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_ParticipantIdsIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string>(),
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
              .WithErrorMessage("Provide at least one participant.");
    }

    [Fact]
    public void Validate_ParticipantIdsContainsDuplicates_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_123" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
              .WithErrorMessage("Duplicate participant IDs are not allowed.");
    }

    [Fact]
    public void Validate_CreatorNotInParticipants_ShouldHaveValidationError()
    {
        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_456", "user_789" },
            File = null
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Creator must be included in ParticipantIds.");
    }

    [Fact]
    public void Validate_FileIsValid_ShouldNotHaveValidationError()
    {
        Mock<Microsoft.AspNetCore.Components.Forms.IBrowserFile> fileMock = new();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<Microsoft.AspNetCore.Components.Forms.IBrowserFile>()))
            .Returns(true);

        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = fileMock.Object
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void Validate_FileIsInvalid_ShouldHaveValidationError()
    {
        Mock<Microsoft.AspNetCore.Components.Forms.IBrowserFile> fileMock = new();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<Microsoft.AspNetCore.Components.Forms.IBrowserFile>()))
            .Returns(false);

        var dto = new UpdateConversationDto
        {
            Title = "Updated Title",
            ConversationId = "conv_123",
            CreatorUserId = "user_123",
            ParticipantIds = new List<string> { "user_123", "user_456" },
            File = fileMock.Object
        };
        TestValidationResult<UpdateConversationDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.File!);
    }
}

