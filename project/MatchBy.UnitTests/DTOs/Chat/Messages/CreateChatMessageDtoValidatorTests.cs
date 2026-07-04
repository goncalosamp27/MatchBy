using FluentValidation.TestHelper;
using MatchBy.DTOs.Chat.Messages;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.Chat.Messages;

public class CreateChatMessageDtoValidatorTests
{
    private readonly CreateChatMessageDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidContent_ShouldPass()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "Hello, World!",
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidInviteUrl_ShouldPass()
    {
        var dto = new CreateChatMessageDto
        {
            Content = null,
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = "https://example.com/invite/123"
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidLocation_ShouldPass()
    {
        var dto = new CreateChatMessageDto
        {
            Content = null,
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = new Location(40.7128, -74.0060, "New York", "USA"),
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNoContentLocationOrInviteUrl_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = null,
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("A message must have either Content, InviteUrl, or Location.");
    }

    [Fact]
    public void Validate_ContentWithInviteUrl_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "Hello",
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = "https://example.com/invite"
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("A message with Content cannot contain InviteUrl or Location.");
    }

    [Fact]
    public void Validate_ContentWithLocation_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "Hello",
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = new Location(0, 0, "City", "Country"),
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("A message with Content cannot contain InviteUrl or Location.");
    }

    [Fact]
    public void Validate_LocationWithInviteUrl_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = null,
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = new Location(0, 0, "City", "Country"),
            InviteUrl = "https://example.com/invite"
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("A message with Location cannot contain Content or InviteUrl.");
    }

    [Fact]
    public void Validate_ContentExceedsMaxLength_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = new string('a', 501),
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_ContentIsWhitespaceOnly_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "   ",
            CreatorUserId = "user_123",
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Content)
              .WithErrorMessage("Content cannot be whitespace only.");
    }

    [Fact]
    public void Validate_CreatorUserIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "Hello",
            CreatorUserId = string.Empty,
            ConversationId = "conv_456",
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CreatorUserId)
              .WithErrorMessage("CreatorUserId is required.");
    }

    [Fact]
    public void Validate_ConversationIdIsEmpty_ShouldHaveValidationError()
    {
        var dto = new CreateChatMessageDto
        {
            Content = "Hello",
            CreatorUserId = "user_123",
            ConversationId = string.Empty,
            ReplyToMessageId = null,
            Location = null,
            InviteUrl = null
        };
        TestValidationResult<CreateChatMessageDto>? result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ConversationId)
              .WithErrorMessage("ConversationId is required.");
    }
}

