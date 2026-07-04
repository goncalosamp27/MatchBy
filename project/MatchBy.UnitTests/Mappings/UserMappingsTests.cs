using MatchBy.DTOs.User;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.UnitTests.Mappings;

public class UserMappingsTests
{
    [Fact]
    public void ToDto_WhenApplicationUserIsValid_ShouldMapToDto()
    {
        // Arrange
        ApplicationUser user = CreateValidUser();

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.DisplayName, dto.DisplayName);
        Assert.Equal(user.ProfileImage?.Url, dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_WhenProfileImageIsNull_ShouldMapNullAvatarUrl()
    {
        // Arrange
        ApplicationUser user = CreateValidUser();
        user.ProfileImage = null;

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.Equal("/images/user-avatar.svg", dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_WhenProfileImageIsNotNull_ShouldMapProfileImageUrl()
    {
        // Arrange
        ApplicationUser user = CreateValidUser();
        var profileImage = new FileStore("https://example.com/profile.jpg", DateTime.UtcNow.AddDays(1), "profile-key", FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = profileImage;

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.Equal("https://example.com/profile.jpg", dto.AvatarUrl);
    }
    
    [Fact]
    public void ToDto_WhenProfileImageUrlIsNull_ShouldMapNullAvatarUrl()
    {
        // Arrange
        ApplicationUser user = CreateValidUser();
        var profileImage = new FileStore(null!, DateTime.UtcNow.AddDays(1), "profile-key", FileCategory.ProfileImage, FileType.Image, DateTime.UtcNow);
        user.ProfileImage = profileImage;

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.Null(dto.AvatarUrl);
    }

    [Fact]
    public void ToDto_WhenDisplayNameIsDifferentFromUserName_ShouldUseUserName()
    {
        // Arrange
        ApplicationUser user = CreateValidUser();
        user.UserName = "username";
        user.DisplayName = "Display Name";

        // Act
        UserDto dto = user.ToDto();

        // Assert
        Assert.Equal("Display Name", dto.DisplayName);
        Assert.Equal(user.DisplayName, dto.DisplayName);
    }

    private static ApplicationUser CreateValidUser()
    {
        return new ApplicationUser
        {
            Id = "user-id",
            UserName = "testuser",
            DisplayName = "Test User",
            ProfileImage = new FileStore(
                Url: "https://example.com/image.jpg",
                ExpireDateTimeUtc: DateTime.UtcNow.AddDays(1),
                Key: "file-key",
                FileCategory: FileCategory.ProfileImage,
                FileType: FileType.Image,
                CreatedAtUtc: DateTime.UtcNow
            ),
        };
    }
}



