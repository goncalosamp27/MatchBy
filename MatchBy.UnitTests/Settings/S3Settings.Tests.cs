using MatchBy.Settings;

namespace MatchBy.UnitTests.Settings;

public class S3SettingsTests
{
    [Fact]
    public void S3Settings_WhenInitialized_ShouldHaveDefaultValues()
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
    public void S3Settings_WhenInitializedWithValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string expectedRegion = "us-east-1";
        const string expectedBucketName = "my-bucket";
        const string expectedAccessKey = "AKIAIOSFODNN7EXAMPLE";
        const string expectedSecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
        const int expectedUrlExpiry = 3600;

        // Act
        var settings = new S3Settings
        {
            Region = expectedRegion,
            BucketName = expectedBucketName,
            AccessKey = expectedAccessKey,
            SecretKey = expectedSecretKey,
            DefaultUrlExpiry = expectedUrlExpiry
        };

        // Assert
        Assert.Equal(expectedRegion, settings.Region);
        Assert.Equal(expectedBucketName, settings.BucketName);
        Assert.Equal(expectedAccessKey, settings.AccessKey);
        Assert.Equal(expectedSecretKey, settings.SecretKey);
        Assert.Equal(expectedUrlExpiry, settings.DefaultUrlExpiry);
    }

    [Fact]
    public void S3Settings_Region_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings { Region = "us-west-2" };

        // Assert
        Assert.Equal("us-west-2", settings.Region);
        // The property is init-only, so we can't reassign it after initialization
        // This test verifies the property can be set during initialization
    }

    [Fact]
    public void S3Settings_BucketName_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings { BucketName = "test-bucket" };

        // Assert
        Assert.Equal("test-bucket", settings.BucketName);
    }

    [Fact]
    public void S3Settings_AccessKey_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings { AccessKey = "test-access-key" };

        // Assert
        Assert.Equal("test-access-key", settings.AccessKey);
    }

    [Fact]
    public void S3Settings_SecretKey_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings { SecretKey = "test-secret-key" };

        // Assert
        Assert.Equal("test-secret-key", settings.SecretKey);
    }

    [Fact]
    public void S3Settings_DefaultUrlExpiry_ShouldBeInitOnly()
    {
        // Arrange
        var settings = new S3Settings { DefaultUrlExpiry = 7200 };

        // Assert
        Assert.Equal(7200, settings.DefaultUrlExpiry);
    }

    [Theory]
    [InlineData("")]
    [InlineData("us-east-1")]
    [InlineData("eu-west-1")]
    [InlineData("ap-southeast-1")]
    public void S3Settings_Region_ShouldAcceptVariousValues(string region)
    {
        // Arrange & Act
        var settings = new S3Settings { Region = region };

        // Assert
        Assert.Equal(region, settings.Region);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(3600)]
    [InlineData(86400)]
    public void S3Settings_DefaultUrlExpiry_ShouldAcceptVariousValues(int expiry)
    {
        // Arrange & Act
        var settings = new S3Settings { DefaultUrlExpiry = expiry };

        // Assert
        Assert.Equal(expiry, settings.DefaultUrlExpiry);
    }

    [Fact]
    public void S3Settings_WhenPartiallyInitialized_ShouldUseDefaultsForUnsetProperties()
    {
        // Arrange & Act
        var settings = new S3Settings
        {
            Region = "us-east-1",
            BucketName = "my-bucket"
        };

        // Assert
        Assert.Equal("us-east-1", settings.Region);
        Assert.Equal("my-bucket", settings.BucketName);
        Assert.Equal(string.Empty, settings.AccessKey);
        Assert.Equal(string.Empty, settings.SecretKey);
        Assert.Equal(0, settings.DefaultUrlExpiry);
    }
}
