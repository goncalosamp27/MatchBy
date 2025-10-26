using FluentValidation;
using MatchBy.Enums;
using MatchBy.Models;

namespace MatchBy.Validators;

public class FileStoreValidator : AbstractValidator<FileStore>
{
    public FileStoreValidator()
    {
        RuleFor(i => i.Url)
            .MaximumLength(2048).WithMessage("Url cannot exceed 2048 characters.");

        RuleFor(i => i.Key)
            .NotEmpty().WithMessage("Key is required.")
            .MaximumLength(2048).WithMessage("Key cannot exceed 2048 characters.");

        RuleFor(i => i.StorageType)
            .NotEmpty().WithMessage("StorageType is required.");

        When(i => i.StorageType == StorageType.S3, () =>
        {
            RuleFor(i => i.ExpireDateTime)
                .NotNull().WithMessage("ExpireDateTime is required when using S3 storage.")
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("ExpireDateTime must be in the future.");
        });

        When(i => i.StorageType == StorageType.Local, () =>
        {
            RuleFor(i => i.ExpireDateTime)
                .Must(date => date == null || date > DateTime.UtcNow)
                .WithMessage("ExpireDateTime must be in the future if provided.");
        });

        RuleFor(i => i.FileType)
            .IsInEnum().WithMessage("Invalid file type.")
            .NotNull().WithMessage("FileType is required.");

        RuleFor(i => i.FileCategory)
            .IsInEnum().WithMessage("Invalid file category.")
            .NotNull().WithMessage("FileCategory is required.");
        
        When(i => i.FileType == FileType.Image, () =>
        {
            RuleFor(i => i.FileCategory)
                .Must(fc => fc == FileCategory.ProfileImage || fc == FileCategory.MatchImage)
                .WithMessage("Invalid file category for an image.");
        });
        
        When(i => i.FileType == FileType.Video, () =>
        {
            RuleFor(i => i.FileCategory)
                .Must(fc => fc != FileCategory.ProfileImage)
                .WithMessage("Videos cannot be used as profile images.");
        });
    }
}
