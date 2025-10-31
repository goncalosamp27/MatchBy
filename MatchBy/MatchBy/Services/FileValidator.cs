using Microsoft.AspNetCore.Components.Forms;
namespace MatchBy.Services;

public class FileValidator(ILogger<FileValidator> logger): IFileValidator
{
    private const long MaxFileBytes = 50 * 1024 * 1024; // 50 MB

    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png"];
    private static readonly string[] AllowedVideoExtensions = [".mp4"];
    private static readonly string[] AllowedVideoTypes = ["video/mp4"];

    // -------- IFormFile --------

    public bool IsValidFormImage(IFormFile file)
    {
        if (file.Length is 0 or > MaxFileBytes)
        {
            logger.LogWarning("Image rejected: invalid size ({SizeBytes} bytes).", file.Length);
            return false;
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            logger.LogWarning("Image rejected: extension '{Ext}' not allowed.", ext);
            return false;
        }

        string ct = file.ContentType.ToLowerInvariant();
        if (!AllowedImageTypes.Contains(ct))
        {
            logger.LogWarning("Image rejected: content type '{Type}' not allowed.", file.ContentType);
            return false;
        }

        logger.LogInformation("Image '{File}' accepted ({SizeBytes} bytes, {Type}).",
            file.FileName, file.Length, file.ContentType);

        return true;
    }

    public bool IsValidFormVideo(IFormFile file)
    {
        if (file.Length is 0 or > MaxFileBytes)
        {
            logger.LogWarning("Video rejected: invalid size ({SizeBytes} bytes).", file.Length);
            return false;
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(ext))
        {
            logger.LogWarning("Video rejected: extension '{Ext}' not allowed.", ext);
            return false;
        }

        string ct = file.ContentType.ToLowerInvariant();
        if (!AllowedVideoTypes.Contains(ct))
        {
            logger.LogWarning("Video rejected: content type '{Type}' not allowed.", file.ContentType);
            return false;
        }

        logger.LogInformation("Video '{File}' accepted ({SizeBytes} bytes, {Type}).",
            file.FileName, file.Length, file.ContentType);

        return true;
    }

    // -------- IBrowserFile --------

    public bool IsValidBrowserImage(IBrowserFile file)
    {
        if (file.Size is 0 or > MaxFileBytes)
        {
            logger.LogWarning("Image rejected: invalid size ({SizeBytes} bytes).", file.Size);
            return false;
        }

        string ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
        {
            logger.LogWarning("Image rejected: extension '{Ext}' not allowed.", ext);
            return false;
        }

        string ct = file.ContentType.ToLowerInvariant();
        if (!AllowedImageTypes.Contains(ct))
        {
            logger.LogWarning("Image rejected: content type '{Type}' not allowed.", file.ContentType);
            return false;
        }

        logger.LogInformation("Image '{File}' accepted ({SizeBytes} bytes, {Type}).",
            file.Name, file.Size, file.ContentType);

        return true;
    }

    public bool IsValidBrowserVideo(IBrowserFile file)
    {

        if (file.Size is 0 or > MaxFileBytes)
        {
            logger.LogWarning("Video rejected: invalid size ({SizeBytes} bytes).", file.Size);
            return false;
        }

        string ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedVideoExtensions.Contains(ext))
        {
            logger.LogWarning("Video rejected: extension '{Ext}' not allowed.", ext);
            return false;
        }

        string ct = file.ContentType.ToLowerInvariant();
        if (!AllowedVideoTypes.Contains(ct))
        {
            logger.LogWarning("Video rejected: content type '{Type}' not allowed.", file.ContentType);
            return false;
        }

        logger.LogInformation("Video '{File}' accepted ({SizeBytes} bytes, {Type}).",
            file.Name, file.Size, file.ContentType);

        return true;
    }
}
