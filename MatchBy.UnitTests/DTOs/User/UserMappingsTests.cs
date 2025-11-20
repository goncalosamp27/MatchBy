using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.DTOs.User;

public class UserMappingsTests
{
    [Fact]
    public void ToDto_WithCompleteApplicationUser_ShouldMapAllProperties()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user_123",
            UserName = "johndoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            ProfileImage = new FileStore(
                Url: "https://example.com/avatar.jpg",
                ExpireDateTimeUtc: DateTime.UtcNow.AddDays(30),
                Key: "profile/user_123",
                FileCategory: FileCategory.ProfileImage,
                FileType: FileType.Image,
                CreatedAtUtc: DateTime.UtcNow
            ),
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.UserName, dto.DisplayName);
        Assert.Equal(user.ProfileImage.Url, dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_WithNullProfileImage_ShouldMapAvatarUrlAsNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user_123",
            UserName = "johndoe",
            DisplayName = "John Doe",
            Email = "john@example.com",
            ProfileImage = null,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.UserName, dto.DisplayName);
        Assert.Null(dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_WithMinimalApplicationUser_ShouldMapRequiredProperties()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user_456",
            UserName = "janedoe",
            Email = "jane@example.com",
            DisplayName = "Jane Doe",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal("user_456", dto.Id);
        Assert.Equal("janedoe", dto.DisplayName);
        Assert.Null(dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_PreservesUserIdentity()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            Id = "user_1",
            UserName = "user1",
            DisplayName = "User One",
            CreatedAtUtc = DateTime.UtcNow
        };

        var user2 = new ApplicationUser
        {
            Id = "user_2",
            UserName = "user2",
            DisplayName = "User Two",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Act
        UserDto dto1 = user1.ToDto();
        UserDto dto2 = user2.ToDto();

        // Assert
        Assert.NotEqual(dto1.Id, dto2.Id);
        Assert.NotEqual(dto1.DisplayName, dto2.DisplayName);
    }
}

