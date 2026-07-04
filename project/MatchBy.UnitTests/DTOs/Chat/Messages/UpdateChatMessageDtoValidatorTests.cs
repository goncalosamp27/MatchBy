using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Messages;

namespace MatchBy.UnitTests.DTOs.Chat.Messages;

public class UpdateChatMessageDtoValidatorTests
{
    private readonly UpdateChatMessageDtoValidator _validator = new();

    [Fact]
    public void Validate_AllFieldsValid_ShouldPass()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = "message_123",
            Content = "Updated content",
            CreatorUserId = "user_123"
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ChatMessageIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = string.Empty,
            Content = "Updated content",
            CreatorUserId = "user_123"
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ChatMessageId)
              .WithErrorMessage("ChatMessageId is required.");
    }

    [Fact]
    public void Validate_ContentIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = "message_123",
            Content = string.Empty,
            CreatorUserId = "user_123"
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content is required.");
    }

    [Fact]
    public void Validate_ContentIsWhitespaceOnly_ShouldHaveValidationError()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = "message_123",
            Content = "   ",
            CreatorUserId = "user_123"
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content cannot be whitespace only.");
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldHaveValidationError()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = "message_123",
            Content = new string('a', 501),
            CreatorUserId = "user_123"
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_CreatorUserIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new UpdateChatMessageDto
        {
            ChatMessageId = "message_123",
            Content = "Updated content",
            CreatorUserId = string.Empty
        };
        TestValidationResult<UpdateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
              .WithErrorMessage("CreatorUserId is required.");
    }
}

