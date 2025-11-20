using MatchBy.Enums;
using MatchBy.Extensions;
using MatchBy.Models;

namespace MatchBy.UnitTests.Extensions;

public class ApplicationUserExtensionsTests
{
    [Fact]
    public void InitializeNewUser_WhenDisplayNameIsValid_ShouldInitializeUser()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal(0, result.Rating);
        Assert.Equal(AccountStatus.Available, result.Status);
        Assert.NotEqual(default, result.CreatedAtUtc);
    }

    [Fact]
    public void InitializeNewUser_WhenDisplayNameIsEmpty_ShouldSetEmptyDisplayName()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        ApplicationUser result = user.InitializeNewUser(string.Empty);

        // Assert
        Assert.Equal(string.Empty, result.DisplayName);
        Assert.Equal(0, result.Rating);
        Assert.Equal(AccountStatus.Available, result.Status);
    }

    [Fact]
    public void InitializeNewUser_WhenDisplayNameIsNull_ShouldSetNullDisplayName()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        ApplicationUser result = user.InitializeNewUser(null!);

        // Assert
        Assert.Null(result.DisplayName);
        Assert.Equal(0, result.Rating);
        Assert.Equal(AccountStatus.Available, result.Status);
    }

    [Fact]
    public void InitializeNewUser_ShouldSetRatingToZero()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Rating = 0f
        };

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.Equal(0, result.Rating);
    }

    [Fact]
    public void InitializeNewUser_ShouldSetStatusToAvailable()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Status = AccountStatus.Available
        };

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.Equal(AccountStatus.Available, result.Status);
    }

    [Fact]
    public void InitializeNewUser_ShouldSetCreatedAtUtc()
    {
        // Arrange
        var user = new ApplicationUser();
        DateTime beforeInitialization = DateTime.UtcNow;

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.NotEqual(default, result.CreatedAtUtc);
        Assert.True(result.CreatedAtUtc >= beforeInitialization);
        Assert.True(result.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void InitializeNewUser_ShouldReturnSameInstance()
    {
        // Arrange
        var user = new ApplicationUser();

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.Same(user, result);
    }

    [Fact]
    public void InitializeNewUser_ShouldPreserveOtherProperties()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-id",
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act
        ApplicationUser result = user.InitializeNewUser("Test User");

        // Assert
        Assert.Equal("user-id", result.Id);
        Assert.Equal("testuser", result.UserName);
        Assert.Equal("test@example.com", result.Email);
    }
}

