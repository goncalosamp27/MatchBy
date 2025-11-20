using MatchBy.DTOs.Chat.Conversations;

namespace MatchBy.UnitTests.Mappings;

public class ConversationCursorDtoTests
{
    [Fact]
    public void Encode_WhenIdAndDateAreValid_ShouldReturnEncodedString()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime date = DateTime.UtcNow;

        // Act
        string encoded = ConversationCursorDto.Encode(id, date);

        // Assert
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Encode_WhenDateIsMinValue_ShouldEncodeSuccessfully()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime date = DateTime.MinValue;

        // Act
        string encoded = ConversationCursorDto.Encode(id, date);

        // Assert
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Encode_WhenDateIsMaxValue_ShouldEncodeSuccessfully()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime date = DateTime.MaxValue;

        // Act
        string encoded = ConversationCursorDto.Encode(id, date);

        // Assert
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsValid_ShouldReturnConversationCursorDto()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime date = DateTime.UtcNow;
        string encoded = ConversationCursorDto.Encode(id, date);

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(id, decoded.Id);
        Assert.Equal(date, decoded.Date);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsNull_ShouldReturnNull()
    {
        // Arrange
        string? encoded = null;

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsEmpty_ShouldReturnNull()
    {
        // Arrange
        string encoded = string.Empty;

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsWhitespace_ShouldReturnNull()
    {
        // Arrange
        const string encoded = "   ";

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsInvalid_ShouldReturnNull()
    {
        // Arrange
        const string encoded = "invalid-encoded-string!@#$%^&*()";

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_WhenEncodedStringIsMalformed_ShouldReturnNull()
    {
        // Arrange
        const string encoded = "not-a-valid-base64url-string";

        // Act
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void EncodeAndDecode_ShouldBeRoundTrip()
    {
        // Arrange
        const string id = "conversation-id-123";
        DateTime date = DateTime.UtcNow.AddDays(-5);

        // Act
        string encoded = ConversationCursorDto.Encode(id, date);
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(id, decoded.Id);
        Assert.Equal(date, decoded.Date);
    }

    [Fact]
    public void EncodeAndDecode_WithDifferentDates_ShouldPreserveDate()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime[] dates =
        [
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow.AddMonths(1)
        ];

        foreach (DateTime date in dates)
        {
            // Act
            string encoded = ConversationCursorDto.Encode(id, date);
            var decoded = ConversationCursorDto.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(date, decoded.Date);
        }
    }

    [Fact]
    public void EncodeAndDecode_WithDifferentIds_ShouldPreserveId()
    {
        // Arrange
        DateTime date = DateTime.UtcNow;
        string[] ids =
        [
            "conversation-id-1",
            "conversation-id-2",
            "very-long-conversation-id-with-special-characters-1234567890",
            "short-id",
            string.Empty
        ];

        foreach (string id in ids)
        {
            // Act
            string encoded = ConversationCursorDto.Encode(id, date);
            var decoded = ConversationCursorDto.Decode(encoded);

            // Assert
            Assert.NotNull(decoded);
            Assert.Equal(id, decoded.Id);
        }
    }

    [Fact]
    public void Encode_WithSpecialCharactersInId_ShouldEncodeAndDecodeSuccessfully()
    {
        // Arrange
        const string id = "conversation-id-with-special-chars-!@#$%^&*()_+-=[]{}|;:',.<>?";
        DateTime date = DateTime.UtcNow;

        // Act
        string encoded = ConversationCursorDto.Encode(id, date);
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(id, decoded.Id);
        Assert.Equal(date, decoded.Date);
    }

    [Fact]
    public void EncodeAndDecode_WithUtcDateTime_ShouldPreserveUtcKind()
    {
        // Arrange
        const string id = "conversation-id";
        DateTime utcDate = DateTime.UtcNow;

        // Act
        string encoded = ConversationCursorDto.Encode(id, utcDate);
        var decoded = ConversationCursorDto.Decode(encoded);

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(utcDate, decoded.Date);
    }
}

