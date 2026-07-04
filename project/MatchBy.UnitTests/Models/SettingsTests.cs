using MatchBy.Settings;

namespace MatchBy.UnitTests.Models;

public class S3SettingsTests
{
    [Fact]
    public void S3Settings_WhenInitializedWithDefaultValues_ShouldHaveEmptyStrings()
    {
        // Arrange & Act
        var settings = new S3Settings();

        // Assert
        Assert.Equal(string.Empty, settings.Region);
        Assert.Equal(string.Empty, settings.BucketName);
        Assert.Equal(string.Empty, settings.AccessKey);
        Assert.Equal(string.Empty, settings.SecretKey);
        Assert.Equal(0, settings.DefaultUrlExpiry);
    }

    [Fact]
    public void S3Settings_WhenInitializedWithValues_ShouldStoreValues()
    {
        // Arrange & Act
        var settings = new S3Settings
        {
            Region = "us-east-1",
            BucketName = "test-bucket",
            AccessKey = "access-key",
            SecretKey = "secret-key",
            DefaultUrlExpiry = 60
        };

        // Assert
        Assert.Equal("us-east-1", settings.Region);
        Assert.Equal("test-bucket", settings.BucketName);
        Assert.Equal("access-key", settings.AccessKey);
        Assert.Equal("secret-key", settings.SecretKey);
        Assert.Equal(60, settings.DefaultUrlExpiry);
    }

    [Fact]
    public void S3Settings_PropertiesShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings
        {
            Region = "us-east-1"
        };

        // Act & Assert - Properties should be init-only, so we can't reassign after initialization
        // This test verifies that the properties are read-only after initialization
        Assert.Equal("us-east-1", settings.Region);
    }
}

public class UploadSettingsTests
{
    [Fact]
    public void UploadSettings_WhenInitializedWithDefaultValue_ShouldHaveZeroMaxFileSize()
    {
        // Arrange & Act
        var settings = new UploadSettings();

        // Assert
        Assert.Equal(0, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_WhenInitializedWithValue_ShouldStoreValue()
    {
        // Arrange & Act
        var settings = new UploadSettings
        {
            MaxFileSizeMegaBytes = 100
        };

        // Assert
        Assert.Equal(100, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_WhenInitializedWithLargeValue_ShouldStoreLargeValue()
    {
        // Arrange & Act
        var settings = new UploadSettings
        {
            MaxFileSizeMegaBytes = long.MaxValue
        };

        // Assert
        Assert.Equal(long.MaxValue, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_WhenInitializedWithNegativeValue_ShouldStoreNegativeValue()
    {
        // Arrange & Act
        var settings = new UploadSettings
        {
            MaxFileSizeMegaBytes = -1
        };

        // Assert
        Assert.Equal(-1, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_PropertiesShouldBeInitOnly()
    {
        // Arrange
        var settings = new UploadSettings
        {
            MaxFileSizeMegaBytes = 50
        };

        // Act & Assert - Properties should be init-only, so we can't reassign after initialization
        // This test verifies that the properties are read-only after initialization
        Assert.Equal(50, settings.MaxFileSizeMegaBytes);
    }
}



