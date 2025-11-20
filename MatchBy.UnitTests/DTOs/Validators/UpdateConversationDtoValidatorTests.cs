using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Conversations;
using MatchBy.Services.FileValidator;
using Microsoft.AspNetCore.Components.Forms;
using Moq;

namespace MatchBy.UnitTests.DTOs.Validators;

public class UpdateConversationDtoValidatorTests
{
    private readonly Mock<IFileValidator> _fileValidatorMock;
    private readonly UpdateConversationDtoValidator _validator;

    public UpdateConversationDtoValidatorTests()
    {
        _fileValidatorMock = new Mock<IFileValidator>();
        _fileValidatorMock.Setup(x => x.GetMaxFileBytes()).Returns(50 * 1024 * 1024); // 50 MB
        _validator = new UpdateConversationDtoValidator(_fileValidatorMock.Object);
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleIsNull_ShouldPass()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { Title = null };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenTitleExceeds200Characters_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { Title = new string('a', 201) };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot exceed 200 characters.");
    }

    [Fact]
    public void Validate_WhenTitleIsExactly200Characters_ShouldPass()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { Title = new string('a', 200) };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenConversationIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ConversationId = string.Empty };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId is required.");
    }

    [Fact]
    public void Validate_WhenConversationIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ConversationId = null! };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { CreatorUserId = string.Empty };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { CreatorUserId = null! };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsIsNull_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ParticipantIds = null! };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Participants are required.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ParticipantIds = new List<string>() };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Provide at least one participant.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsEmptyString_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ParticipantIds = new List<string> { "user1", string.Empty, "user2" } };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Some participant IDs are empty.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsWhitespace_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ParticipantIds = new List<string> { "user1", "   ", "user2" } };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Some participant IDs are empty.");
    }

    [Fact]
    public void Validate_WhenParticipantIdsContainsDuplicates_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { ParticipantIds = new List<string> { "user1", "user2", "user1" } };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ParticipantIds)
            .WithErrorMessage("Duplicate participant IDs are not allowed.");
    }

    [Fact]
    public void Validate_WhenCreatorIsNotInParticipantIds_ShouldFail()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { CreatorUserId = "creator", ParticipantIds = new List<string> { "user1", "user2" } };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Creator must be included in ParticipantIds.");
    }

    [Fact]
    public void Validate_WhenCreatorIsInParticipantIds_ShouldPass()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { CreatorUserId = "creator", ParticipantIds = new List<string> { "creator", "user2" } };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void Validate_WhenFileIsNull_ShouldPass()
    {
        // Arrange
        UpdateConversationDto dto = CreateValidDto() with { File = null };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.File);
    }

    [Fact]
    public void Validate_WhenFileIsValidImage_ShouldPass()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<IBrowserFile>())).Returns(true);
        _fileValidatorMock.Setup(x => x.IsValidBrowserVideo(It.IsAny<IBrowserFile>())).Returns(false);
        UpdateConversationDto dto = CreateValidDto() with { File = mockFile.Object };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.File);
    }
   
    [Fact]
    public void Validate_WhenFileIsInvalid_ShouldFail()
    {
        // Arrange
        var mockFile = new Mock<IBrowserFile>();
        _fileValidatorMock.Setup(x => x.IsValidBrowserImage(It.IsAny<IBrowserFile>())).Returns(false);
        _fileValidatorMock.Setup(x => x.IsValidBrowserVideo(It.IsAny<IBrowserFile>())).Returns(false);
        UpdateConversationDto dto = CreateValidDto() with { File = mockFile.Object };

        // Act
        TestValidationResult<UpdateConversationDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.File)
            .WithErrorMessage("File is not allowed. Only .jpg, .jpeg, .png images are accepted, up to 50 MB.");
    }

    private static UpdateConversationDto CreateValidDto()
    {
        return new UpdateConversationDto
        {
            ConversationId = "conversation-id",
            CreatorUserId = "creator",
            ParticipantIds = ["creator", "user2"],
            Title = "Test Conversation"
        };
    }
}


