using Microsoft.AspNetCore.Components.Forms;
using Moq;

namespace MatchBy.UnitTests.Services.FileValidator;

public class FileValidatorTests
{
    private readonly MatchBy.Services.FileValidator.FileValidator _fileValidator;

    public FileValidatorTests()
    {
        var loggerMock = new Mock<ILogger<MatchBy.Services.FileValidator.FileValidator>>();
        _fileValidator = new MatchBy.Services.FileValidator.FileValidator(loggerMock.Object);
    }

    #region GetMaxFileBytes Tests

    [Fact]
    public void GetMaxFileBytes_ShouldReturn50MB()
    {
        // Act
        long result = _fileValidator.GetMaxFileBytes();

        // Assert
        Assert.Equal(50 * 1024 * 1024, result);
    }

    #endregion

    #region IsValidFormImage Tests

    [Fact]
    public void IsValidFormImage_WithValidJpeg_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024 * 1024); // 1 MB
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFormImage_WithValidPng_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024 * 1024); // 1 MB
        fileMock.Setup(f => f.FileName).Returns("test.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFormImage_WithFileTooLarge_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(51 * 1024 * 1024); // 51 MB
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFormImage_WithEmptyFile_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFormImage_WithInvalidExtension_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("test.gif");
        fileMock.Setup(f => f.ContentType).Returns("image/gif");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFormImage_WithInvalidContentType_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/gif");

        // Act
        bool result = _fileValidator.IsValidFormImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidFormVideo Tests

    [Fact]
    public void IsValidFormVideo_WithValidMp4_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10 MB
        fileMock.Setup(f => f.FileName).Returns("test.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");

        // Act
        bool result = _fileValidator.IsValidFormVideo(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFormVideo_WithFileTooLarge_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(51 * 1024 * 1024); // 51 MB
        fileMock.Setup(f => f.FileName).Returns("test.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");

        // Act
        bool result = _fileValidator.IsValidFormVideo(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFormVideo_WithInvalidExtension_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(10 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("test.avi");
        fileMock.Setup(f => f.ContentType).Returns("video/avi");

        // Act
        bool result = _fileValidator.IsValidFormVideo(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidBrowserImage Tests

    [Fact]
    public void IsValidBrowserImage_WithValidJpeg_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(1024 * 1024); // 1 MB
        fileMock.Setup(f => f.Name).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidBrowserImage(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidBrowserImage_WithValidPng_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(1024 * 1024); // 1 MB
        fileMock.Setup(f => f.Name).Returns("test.png");
        fileMock.Setup(f => f.ContentType).Returns("image/png");

        // Act
        bool result = _fileValidator.IsValidBrowserImage(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidBrowserImage_WithFileTooLarge_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(51 * 1024 * 1024); // 51 MB
        fileMock.Setup(f => f.Name).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidBrowserImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidBrowserImage_WithEmptyFile_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(0);
        fileMock.Setup(f => f.Name).Returns("test.jpg");
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        bool result = _fileValidator.IsValidBrowserImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidBrowserImage_WithInvalidExtension_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(1024 * 1024);
        fileMock.Setup(f => f.Name).Returns("test.gif");
        fileMock.Setup(f => f.ContentType).Returns("image/gif");

        // Act
        bool result = _fileValidator.IsValidBrowserImage(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsValidBrowserVideo Tests

    [Fact]
    public void IsValidBrowserVideo_WithValidMp4_ShouldReturnTrue()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(10 * 1024 * 1024); // 10 MB
        fileMock.Setup(f => f.Name).Returns("test.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");

        // Act
        bool result = _fileValidator.IsValidBrowserVideo(fileMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidBrowserVideo_WithFileTooLarge_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(51 * 1024 * 1024); // 51 MB
        fileMock.Setup(f => f.Name).Returns("test.mp4");
        fileMock.Setup(f => f.ContentType).Returns("video/mp4");

        // Act
        bool result = _fileValidator.IsValidBrowserVideo(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidBrowserVideo_WithInvalidExtension_ShouldReturnFalse()
    {
        // Arrange
        var fileMock = new Mock<IBrowserFile>();
        fileMock.Setup(f => f.Size).Returns(10 * 1024 * 1024);
        fileMock.Setup(f => f.Name).Returns("test.avi");
        fileMock.Setup(f => f.ContentType).Returns("video/avi");

        // Act
        bool result = _fileValidator.IsValidBrowserVideo(fileMock.Object);

        // Assert
        Assert.False(result);
    }

    #endregion
}






