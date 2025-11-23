using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Messages;

namespace MatchBy.UnitTests.DTOs.Validators;

public class UpdateChatMessageDtoValidatorTests
{
    private readonly UpdateChatMessageDtoValidator _validator;

    public UpdateChatMessageDtoValidatorTests()
    {
        _validator = new UpdateChatMessageDtoValidator();
    }

    [Fact]
    public void Validate_WhenDtoIsValid_ShouldPass()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto();

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenChatMessageIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { ChatMessageId = string.Empty };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatMessageId)
            .WithErrorMessage("ChatMessageId is required.");
    }

    [Fact]
    public void Validate_WhenChatMessageIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { ChatMessageId = null! };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatMessageId)
            .WithErrorMessage("ChatMessageId is required.");
    }

    [Fact]
    public void Validate_WhenChatMessageIdExceeds500Characters_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { ChatMessageId = new string('a', 501) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChatMessageId)
            .WithErrorMessage("ChatMessageId must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenChatMessageIdIsExactly500Characters_ShouldPass()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { ChatMessageId = new string('a', 500) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChatMessageId);
    }

    [Fact]
    public void Validate_WhenContentIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { Content = string.Empty };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_WhenContentIsNull_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { Content = null! };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_WhenContentIsWhitespaceOnly_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { Content = "   " };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content cannot be whitespace only.");
    }

    [Fact]
    public void Validate_WhenContentExceeds500Characters_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { Content = new string('a', 501) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenContentIsExactly500Characters_ShouldPass()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { Content = new string('a', 500) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsEmpty_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { CreatorUserId = string.Empty };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsNull_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { CreatorUserId = null! };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdExceeds500Characters_ShouldFail()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { CreatorUserId = new string('a', 501) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsExactly500Characters_ShouldPass()
    {
        // Arrange
        UpdateChatMessageDto dto = CreateValidDto() with { CreatorUserId = new string('a', 500) };

        // Act
        TestValidationResult<UpdateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CreatorUserId);
    }

    private static UpdateChatMessageDto CreateValidDto()
    {
        return new UpdateChatMessageDto
        {
            ChatMessageId = "message-id",
            Content = "Updated message content",
            CreatorUserId = "creator-id"
        };
    }
}


