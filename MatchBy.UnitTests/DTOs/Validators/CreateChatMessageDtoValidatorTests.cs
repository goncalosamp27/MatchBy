using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Validators;

public class CreateChatMessageDtoValidatorTests
{
    private readonly CreateChatMessageDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenDtoWithContentIsValid_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto();

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDtoWithInviteUrlIsValid_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidInviteUrlDto();

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDtoWithLocationIsValid_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidLocationDto();

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenContentIsEmptyAndNoOtherFields_ShouldFail()
    {
        // Arrange
        var dto = new CreateChatMessageDto
        {
            Content = string.Empty,
            CreatorUserId = "creator",
            ConversationId = "conversation-id"
        };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_WhenContentIsWhitespaceOnly_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { Content = "   " };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content cannot be whitespace only.");
    }

    [Fact]
    public void Validate_WhenContentExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { Content = new string('a', 501) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenContentIsExactly500Characters_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { Content = new string('a', 500) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsEmpty_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { CreatorUserId = string.Empty };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdIsNull_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { CreatorUserId = null! };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_WhenCreatorUserIdExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { CreatorUserId = new string('a', 501) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
            .WithErrorMessage("CreatorUserId must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenConversationIdIsEmpty_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { ConversationId = string.Empty };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId is required.");
    }

    [Fact]
    public void Validate_WhenConversationIdIsNull_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { ConversationId = null! };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId is required.");
    }

    [Fact]
    public void Validate_WhenConversationIdExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { ConversationId = new string('a', 501) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
            .WithErrorMessage("ConversationId must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenReplyToMessageIdExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { ReplyToMessageId = new string('a', 501) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReplyToMessageId)
            .WithErrorMessage("ReplyToMessageId must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenReplyToMessageIdIsNull_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { ReplyToMessageId = null };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReplyToMessageId);
    }

    [Fact]
    public void Validate_WhenInviteUrlExceeds500Characters_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidInviteUrlDto() with { InviteUrl = new string('a', 501) };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InviteUrl)
            .WithErrorMessage("InviteUrl must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WhenInviteUrlIsNull_ShouldPass()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { InviteUrl = null };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InviteUrl);
    }

    [Fact]
    public void Validate_WhenInviteUrlHasContent_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidInviteUrlDto() with { Content = "Some content" };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with InviteUrl cannot contain Content or Location.");
    }

    [Fact]
    public void Validate_WhenInviteUrlHasLocation_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidInviteUrlDto() with { Location = new Location(40.7128, -74.0060, "New York", "USA") };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with InviteUrl cannot contain Content or Location.");
    }

    [Fact]
    public void Validate_WhenLocationHasContent_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidLocationDto() with { Content = "Some content" };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with Location cannot contain Content or InviteUrl.");
    }

    [Fact]
    public void Validate_WhenLocationHasInviteUrl_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidLocationDto() with { InviteUrl = "https://example.com/invite" };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with Location cannot contain Content or InviteUrl.");
    }

    [Fact]
    public void Validate_WhenContentHasInviteUrl_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { InviteUrl = "https://example.com/invite" };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with Content cannot contain InviteUrl or Location.");
    }

    [Fact]
    public void Validate_WhenContentHasLocation_ShouldFail()
    {
        // Arrange
        CreateChatMessageDto dto = CreateValidContentDto() with { Location = new Location(40.7128, -74.0060, "New York", "USA") };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message with Content cannot contain InviteUrl or Location.");
    }

    [Fact]
    public void Validate_WhenNoFieldsAreProvided_ShouldFail()
    {
        // Arrange
        var dto = new CreateChatMessageDto
        {
            Content = null,
            CreatorUserId = "creator",
            ConversationId = "conversation-id",
            InviteUrl = null,
            Location = null
        };

        // Act
        TestValidationResult<CreateChatMessageDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("A message must have either Content, InviteUrl, or Location.");
    }

    private static CreateChatMessageDto CreateValidContentDto()
    {
        return new CreateChatMessageDto
        {
            Content = "Test message content",
            CreatorUserId = "creator-id",
            ConversationId = "conversation-id"
        };
    }

    private static CreateChatMessageDto CreateValidInviteUrlDto()
    {
        return new CreateChatMessageDto
        {
            InviteUrl = "https://example.com/invite",
            CreatorUserId = "creator-id",
            ConversationId = "conversation-id"
        };
    }

    private static CreateChatMessageDto CreateValidLocationDto()
    {
        return new CreateChatMessageDto
        {
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            CreatorUserId = "creator-id",
            ConversationId = "conversation-id"
        };
    }
}


