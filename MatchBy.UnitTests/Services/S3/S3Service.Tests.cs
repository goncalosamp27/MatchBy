using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MatchBy.UnitTests.Services.S3;

public class S3ServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly S3Service _s3Service;
    private readonly S3Settings _s3Settings;
    private readonly UploadSettings _uploadSettings;

    public S3ServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        var loggerMock = new Mock<ILogger<S3Service>>();
        var s3SettingsMock = new Mock<IOptions<S3Settings>>();
        var uploadSettingsMock = new Mock<IOptions<UploadSettings>>();

        _s3Settings = new S3Settings
        {
            BucketName = "test-bucket",
            Region = "us-east-1",
            AccessKey = "test-key",
            SecretKey = "test-secret",
            DefaultUrlExpiry = 30
        };

        _uploadSettings = new UploadSettings
        {
            MaxFileSizeMegaBytes = 50
        };

        s3SettingsMock.Setup(x => x.Value).Returns(_s3Settings);
        uploadSettingsMock.Setup(x => x.Value).Returns(_uploadSettings);

        _s3Service = new S3Service(
            _s3ClientMock.Object,
            s3SettingsMock.Object,
            loggerMock.Object,
            uploadSettingsMock.Object
        );
    }

    #region UploadFormFileAsync Tests

    [Fact]
    public async Task UploadFormFileAsync_WithValidFile_ShouldReturnSuccessWithKey()
    {
        // Arrange
        string fileName = "test-image.jpg";
        string contentType = "image/jpeg";
        string folder = "users/user1/profile-pictures";
        byte[] fileContent = "test file content"u8.ToArray();

        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns(fileName);
        formFileMock.Setup(f => f.ContentType).Returns(contentType);
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fileContent));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        Result<string> result = await _s3Service.UploadFormFileAsync(formFileMock.Object, folder);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.EndsWith(".jpg", result.Data);

        _s3ClientMock.Verify(
            x => x.PutObjectAsync(
                It.Is<PutObjectRequest>(r =>
                    r.BucketName == _s3Settings.BucketName &&
                    r.Key.StartsWith(folder + "/") &&
                    r.ContentType == contentType),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task UploadFormFileAsync_WhenS3Returns_NonOKStatus_ShouldReturnFailure()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("test.jpg");
        formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream("test"u8.ToArray()));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.BadRequest });

        // Act
        Result<string> result = await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File upload failed", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task UploadFormFileAsync_WhenS3ThrowsAmazonS3Exception_ShouldReturnFailure()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("test.jpg");
        formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream("test"u8.ToArray()));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new AmazonS3Exception("S3 error"));

        // Act
        Result<string> result = await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("AWS S3 error", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task UploadFormFileAsync_WhenUnexpectedExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("test.jpg");
        formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream("test"u8.ToArray()));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        Result<string> result = await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unexpected error", result.ErrorMessages[0]);
    }

    #endregion

    #region UploadBrowserFileAsync Tests

    [Fact]
    public async Task UploadBrowserFileAsync_WithValidFile_ShouldReturnSuccessWithKey()
    {
        // Arrange
        string fileName = "browser-image.png";
        string contentType = "image/png";
        string folder = "teams/team1/image";
        byte[] fileContent = "test browser file content"u8.ToArray();

        var browserFileMock = new Mock<IBrowserFile>();
        browserFileMock.Setup(f => f.Name).Returns(fileName);
        browserFileMock.Setup(f => f.ContentType).Returns(contentType);
        browserFileMock.Setup(f => f.OpenReadStream(It.IsAny<long>(), default))
            .Returns(new MemoryStream(fileContent));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        Result<string> result = await _s3Service.UploadBrowserFileAsync(browserFileMock.Object, folder);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.EndsWith(".png", result.Data);

        browserFileMock.Verify(
            x => x.OpenReadStream(_uploadSettings.MaxFileSizeMegaBytes * 1024 * 1024, default),
            Times.Once);
    }

    [Fact]
    public async Task UploadBrowserFileAsync_WithDifferentFileExtensions_ShouldPreserveExtension()
    {
        // Arrange
        (string, string)[] testCases = new[]
        {
            ("image.jpg", ".jpg"),
            ("document.pdf", ".pdf"),
            ("video.mp4", ".mp4"),
            ("file.TXT", ".txt") // Test case insensitivity
        };

        foreach (var (fileName, expectedExtension) in testCases)
        {
            var browserFileMock = new Mock<IBrowserFile>();
            browserFileMock.Setup(f => f.Name).Returns(fileName);
            browserFileMock.Setup(f => f.ContentType).Returns("application/octet-stream");
            browserFileMock.Setup(f => f.OpenReadStream(It.IsAny<long>(), CancellationToken.None))
                .Returns(new MemoryStream("test"u8.ToArray()));

            _s3ClientMock
                .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
                .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

            // Act
            Result<string> result = await _s3Service.UploadBrowserFileAsync(browserFileMock.Object, "folder");

            // Assert
            Assert.True(result.Success);
            Assert.EndsWith(expectedExtension, result.Data);
        }
    }

    #endregion

    #region GetPresignedUrlAsync Tests

    [Fact]
    public async Task GetPresignedUrlAsync_WithValidKey_ShouldReturnPresignedUrl()
    {
        // Arrange
        string key = "users/user1/profile-pictures/image.jpg";
        string expectedUrl = "https://s3.amazonaws.com/presigned-url";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        Result<string> result = await _s3Service.GetPresignedUrlAsync(key, HttpVerb.GET);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(expectedUrl, result.Data);

        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(It.Is<GetPreSignedUrlRequest>(r =>
                r.BucketName == _s3Settings.BucketName &&
                r.Key == key &&
                r.Verb == HttpVerb.GET)),
            Times.Once);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WithPUTVerb_ShouldUseCorrectVerb()
    {
        // Arrange
        string key = "test-key";
        string expectedUrl = "https://s3.amazonaws.com/put-url";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ReturnsAsync(expectedUrl);

        // Act
        Result<string> result = await _s3Service.GetPresignedUrlAsync(key, HttpVerb.PUT);

        // Assert
        Assert.True(result.Success);
        _s3ClientMock.Verify(
            x => x.GetPreSignedURLAsync(It.Is<GetPreSignedUrlRequest>(r => r.Verb == HttpVerb.PUT)),
            Times.Once);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WhenS3ThrowsAmazonS3Exception_ShouldReturnFailure()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ThrowsAsync(new AmazonS3Exception("S3 error"));

        // Act
        Result<string> result = await _s3Service.GetPresignedUrlAsync(key, HttpVerb.GET);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("AWS S3 error", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WhenUnexpectedExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.GetPreSignedURLAsync(It.IsAny<GetPreSignedUrlRequest>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        Result<string> result = await _s3Service.GetPresignedUrlAsync(key, HttpVerb.GET);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unexpected error", result.ErrorMessages[0]);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithValidKey_ShouldReturnSuccess()
    {
        // Arrange
        string key = "users/user1/profile-pictures/old-image.jpg";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        // Act
        Result<bool> result = await _s3Service.DeleteFileAsync(key);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);

        _s3ClientMock.Verify(
            x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithOKStatusCode_ShouldReturnSuccess()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        Result<bool> result = await _s3Service.DeleteFileAsync(key);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonSuccessStatusCode_ShouldReturnFailure()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None))
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NotFound });

        // Act
        Result<bool> result = await _s3Service.DeleteFileAsync(key);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File deletion failed", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenS3ThrowsAmazonS3Exception_ShouldReturnFailure()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None))
            .ThrowsAsync(new AmazonS3Exception("S3 error"));

        // Act
        Result<bool> result = await _s3Service.DeleteFileAsync(key);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("AWS S3 error", result.ErrorMessages[0]);
    }

    [Fact]
    public async Task DeleteFileAsync_WhenUnexpectedExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        string key = "test-key";

        _s3ClientMock
            .Setup(x => x.DeleteObjectAsync(_s3Settings.BucketName, key, CancellationToken.None))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        Result<bool> result = await _s3Service.DeleteFileAsync(key);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Unexpected error", result.ErrorMessages[0]);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task UploadFormFileAsync_ShouldIncludeFileNameInMetadata()
    {
        // Arrange
        string fileName = "my-special-file.jpg";
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns(fileName);
        formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream("test"u8.ToArray()));

        PutObjectRequest? capturedRequest = null;
        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
            .Callback<PutObjectRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");

        // Assert
        Assert.NotNull(capturedRequest);
        //Assert.True(capturedRequest.Metadata.ContainsKey("file-name"));
        Assert.Equal(fileName, capturedRequest.Metadata["file-name"]);
    }

    [Fact]
    public async Task UploadFormFileAsync_ShouldGenerateUniqueKeys()
    {
        // Arrange
        var formFileMock = new Mock<IFormFile>();
        formFileMock.Setup(f => f.FileName).Returns("test.jpg");
        formFileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        formFileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream("test"u8.ToArray()));

        _s3ClientMock
            .Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), CancellationToken.None))
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        Result<string> result1 = await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");
        Result<string> result2 = await _s3Service.UploadFormFileAsync(formFileMock.Object, "folder");

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.NotEqual(result1.Data, result2.Data); // Keys should be unique
    }

    #endregion
}
