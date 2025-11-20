using FluentValidation.TestHelper;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Validators;

namespace MatchBy.UnitTests.Models;

public class FileStoreValidatorTests
{
    private readonly FileStoreValidator _validator = new();

    [Fact]
    public void Validate_WhenFileStoreIsValid_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenUrlIsNull_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(null!, fileStore.ExpireDateTimeUtc, fileStore.Key, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void Validate_WhenUrlExceeds2048Characters_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        string longUrl = new('a', 2049);
        fileStore = fileStore with { Url = longUrl };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("Url cannot exceed 2048 characters.");
    }

    [Fact]
    public void Validate_WhenUrlIsExactly2048Characters_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        string url = new string('a', 2048);
        fileStore = new FileStore(url, fileStore.ExpireDateTimeUtc, fileStore.Key, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
    }

    [Fact]
    public void Validate_WhenKeyIsEmpty_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(fileStore.Url, fileStore.ExpireDateTimeUtc, string.Empty, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Key)
            .WithErrorMessage("Key is required.");
    }

    [Fact]
    public void Validate_WhenKeyIsNull_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(fileStore.Url, fileStore.ExpireDateTimeUtc, null!, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Key)
            .WithErrorMessage("Key is required.");
    }

    [Fact]
    public void Validate_WhenKeyExceeds2048Characters_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        string longKey = new string('a', 2049);
        fileStore = new FileStore(fileStore.Url, fileStore.ExpireDateTimeUtc, longKey, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Key)
            .WithErrorMessage("Key cannot exceed 2048 characters.");
    }

    [Fact]
    public void Validate_WhenKeyIsExactly2048Characters_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        string key = new string('a', 2048);
        fileStore = new FileStore(fileStore.Url, fileStore.ExpireDateTimeUtc, key, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Key);
    }

    [Fact]
    public void Validate_WhenExpireDateTimeUtcIsInThePast_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(fileStore.Url, DateTime.UtcNow.AddDays(-1), fileStore.Key, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ExpireDateTimeUtc)
            .WithErrorMessage("ExpireDateTimeUtc must be in the future if provided.");
    }

    [Fact]
    public void Validate_WhenExpireDateTimeUtcIsInTheFuture_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(fileStore.Url, DateTime.UtcNow.AddDays(1), fileStore.Key, fileStore.FileCategory, fileStore.FileType, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ExpireDateTimeUtc);
    }

    [Fact]
    public void Validate_WhenFileTypeIsInvalidEnum_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = new FileStore(fileStore.Url, fileStore.ExpireDateTimeUtc, fileStore.Key, fileStore.FileCategory, (FileType)999, fileStore.CreatedAtUtc);

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileType)
            .WithErrorMessage("Invalid file type.");
    }
    
    [Fact]
    public void Validate_WhenFileCategoryIsInvalidEnum_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = fileStore with { FileCategory = (FileCategory)999 };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileCategory)
            .WithErrorMessage("Invalid file category.");
    }

    
    [Fact]
    public void Validate_WhenFileTypeIsImageAndFileCategoryIsProfileImage_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = fileStore with { FileCategory = FileCategory.ProfileImage, FileType = FileType.Image };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileCategory);
    }

    [Fact]
    public void Validate_WhenFileTypeIsImageAndFileCategoryIsMatchImage_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = fileStore with { FileCategory = FileCategory.MatchImage, FileType = FileType.Image };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileCategory);
    }

    [Fact]
    public void Validate_WhenFileTypeIsVideoAndFileCategoryIsProfileImage_ShouldFail()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = fileStore with { FileCategory = FileCategory.ProfileImage, FileType = FileType.Video };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileCategory)
            .WithErrorMessage("Videos cannot be used as profile images.");
    }

    [Fact]
    public void Validate_WhenFileTypeIsVideoAndFileCategoryIsMatchImage_ShouldPass()
    {
        // Arrange
        FileStore fileStore = CreateValidFileStore();
        fileStore = fileStore with { FileCategory = FileCategory.MatchImage, FileType = FileType.Video };

        // Act
        TestValidationResult<FileStore>? result = _validator.TestValidate(fileStore);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FileCategory);
    }

    private static FileStore CreateValidFileStore()
    {
        return new FileStore(
            "https://example.com/file.jpg",
            DateTime.UtcNow.AddDays(1),
            "test-key",
            FileCategory.ProfileImage,
            FileType.Image,
            DateTime.UtcNow
        );
    }
}


