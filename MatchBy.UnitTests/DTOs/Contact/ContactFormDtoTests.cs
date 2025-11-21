using MatchBy.DTOs.Contact;

namespace MatchBy.UnitTests.DTOs.Contact;

public class ContactFormDtoTests
{
    [Fact]
    public void ContactFormDto_ShouldInitializeWithAllRequiredProperties()
    {
        // Arrange & Act
        ContactFormDto dto = new()
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Assert
        Assert.Equal("John Doe", dto.Name);
        Assert.Equal("john.doe@example.com", dto.Email);
        Assert.Equal("Test Subject", dto.Subject);
        Assert.Equal("Test Message", dto.Message);
    }

    [Fact]
    public void ContactFormDto_ShouldBeRecord_WithValueEquality()
    {
        // Arrange
        ContactFormDto dto1 = new()
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        ContactFormDto dto2 = new()
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        ContactFormDto dto3 = new()
        {
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            Subject = "Different Subject",
            Message = "Different Message"
        };

        // Assert
        Assert.Equal(dto1, dto2); // Records with same values should be equal
        Assert.NotEqual(dto1, dto3); // Records with different values should not be equal
    }

    [Fact]
    public void ContactFormDto_ShouldSupportWithExpression()
    {
        // Arrange
        ContactFormDto originalDto = new()
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Subject = "Test Subject",
            Message = "Test Message"
        };

        // Act
        ContactFormDto modifiedDto = originalDto with { Name = "Jane Doe" };

        // Assert
        Assert.Equal("Jane Doe", modifiedDto.Name);
        Assert.Equal("john.doe@example.com", modifiedDto.Email);
        Assert.NotEqual(originalDto, modifiedDto);
    }
}

