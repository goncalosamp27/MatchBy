using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.Services;

/// <summary>
/// Defines methods for validating uploaded image and video files
/// from both server-side form submissions (<see cref="IFormFile"/>)
/// and Blazor client-side uploads (<see cref="IBrowserFile"/>).
/// </summary>
/// <remarks>
/// Implementations should verify file size, extension, and MIME type at minimum.
/// In production scenarios, additional checks such as magic byte validation,
/// image dimensions, or video metadata may also be appropriate.
/// </remarks>
public interface IFileValidator
{
    /// <summary>
    /// Validates an uploaded image received as an <see cref="IFormFile"/>
    /// (typically from a server-side form submission or API endpoint).
    /// </summary>
    /// <param name="file">The uploaded image file to validate.</param>
    /// <returns>
    /// <c>true</c> if the image meets validation requirements;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsValidFormImage(IFormFile file);

    /// <summary>
    /// Validates an uploaded video received as an <see cref="IFormFile"/>
    /// (typically from a server-side form submission or API endpoint).
    /// </summary>
    /// <param name="file">The uploaded video file to validate.</param>
    /// <returns>
    /// <c>true</c> if the video meets validation requirements;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsValidFormVideo(IFormFile file);

    /// <summary>
    /// Validates an uploaded image received as an <see cref="IBrowserFile"/>
    /// (typically from a Blazor <see cref="InputFile"/> component).
    /// </summary>
    /// <param name="file">The uploaded image file to validate.</param>
    /// <returns>
    /// <c>true</c> if the image meets validation requirements;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsValidBrowserImage(IBrowserFile file);

    /// <summary>
    /// Validates an uploaded video received as an <see cref="IBrowserFile"/>
    /// (typically from a Blazor <see cref="InputFile"/> component).
    /// </summary>
    /// <param name="file">The uploaded video file to validate.</param>
    /// <returns>
    /// <c>true</c> if the video meets validation requirements;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool IsValidBrowserVideo(IBrowserFile file);
}
