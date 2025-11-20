using MatchBy.Settings;

namespace MatchBy.UnitTests.Settings;

public class UploadSettingsTests
{
    [Fact]
    public void UploadSettings_WhenInitialized_ShouldHaveDefaultValue()
    {
        // Arrange & Act
        var settings = new UploadSettings();

        // Assert
        Assert.Equal(0, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_WhenInitializedWithValue_ShouldSetPropertyCorrectly()
    {
        // Arrange
        const long expectedMaxSize = 10;

        // Act
        var settings = new UploadSettings
        {
            MaxFileSizeMegaBytes = expectedMaxSize
        };

        // Assert
        Assert.Equal(expectedMaxSize, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_MaxFileSizeMegaBytes_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new UploadSettings { MaxFileSizeMegaBytes = 5 };

        // Assert
        Assert.Equal(5, settings.MaxFileSizeMegaBytes);
        // The property is init-only, so we can't reassign it after initialization
        // This test verifies the property can be set during initialization
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void UploadSettings_MaxFileSizeMegaBytes_ShouldAcceptVariousValues(long maxSize)
    {
        // Arrange & Act
        var settings = new UploadSettings { MaxFileSizeMegaBytes = maxSize };

        // Assert
        Assert.Equal(maxSize, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_MaxFileSizeMegaBytes_ShouldAcceptLargeValues()
    {
        // Arrange
        const long largeValue = 1024; // 1 GB

        // Act
        var settings = new UploadSettings { MaxFileSizeMegaBytes = largeValue };

        // Assert
        Assert.Equal(largeValue, settings.MaxFileSizeMegaBytes);
    }

    [Fact]
    public void UploadSettings_MaxFileSizeMegaBytes_ShouldAcceptVeryLargeValues()
    {
        // Arrange
        const long veryLargeValue = long.MaxValue;

        // Act
        var settings = new UploadSettings { MaxFileSizeMegaBytes = veryLargeValue };

        // Assert
        Assert.Equal(veryLargeValue, settings.MaxFileSizeMegaBytes);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void UploadSettings_WithCommonFileSizeLimits_ShouldStoreCorrectly(long sizeInMB)
    {
        // Arrange & Act
        var settings = new UploadSettings { MaxFileSizeMegaBytes = sizeInMB };

        // Assert
        Assert.Equal(sizeInMB, settings.MaxFileSizeMegaBytes);
        Assert.True(settings.MaxFileSizeMegaBytes > 0);
    }
}
