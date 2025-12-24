using MatchBy.DTOs.User;

namespace MatchBy.UnitTests.DTOs.User;

public class UserDtoTests
{
    [Fact]
    public void UserDto_ShouldInitializeWithAllRequiredProperties()
    {
        // Arrange & Act
        var dto = new UserDto
        {
            Id = "user_123",
            DisplayName = "John Doe",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Assert
        Assert.Equal("user_123", dto.Id);
        Assert.Equal("John Doe", dto.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", dto.AvatarUrl);
    }

    [Fact]
    public void UserDto_AvatarUrl_CanBeNull()
    {
        // Arrange & Act
        var dto = new UserDto
        {
            Id = "user_123",
            DisplayName = "John Doe",
            AvatarUrl = null
        };

        // Assert
        Assert.Null(dto.AvatarUrl);
    }

    [Fact]
    public void UserDto_ShouldSupportWithExpression()
    {
        // Arrange
        var originalDto = new UserDto
        {
            Id = "user_123",
            DisplayName = "John Doe",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        // Act
        UserDto modifiedDto = originalDto with { DisplayName = "Jane Doe" };

        // Assert
        Assert.Equal("Jane Doe", modifiedDto.DisplayName);
        Assert.Equal("user_123", modifiedDto.Id);
        Assert.NotEqual(originalDto, modifiedDto);
    }
}

