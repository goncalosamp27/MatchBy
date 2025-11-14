using Amazon.S3;
using MatchBy.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace MatchBy.Services.S3;

public interface IS3Service
{
    /// <summary>
    /// Uploads a form file to the specified S3 folder.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="folder">The S3 folder path.</param>
    /// <returns>The generated S3 key for the uploaded file, or null if upload failed.</returns>
    Task<Result<string>> UploadFormFileAsync(
        IFormFile file,
        string folder);

    /// <summary>
    /// Uploads a browser file to the specified S3 folder.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="folder">The S3 folder path.</param>
    /// <returns>The generated S3 key for the uploaded file, or null if upload failed.</returns>
    Task<Result<string>> UploadBrowserFileAsync(
        IBrowserFile file,
        string folder);
    
    /// <summary>
    /// Generates a pre-signed URL that allows temporary access to a file in S3.
    /// </summary>
    /// <param name="key">The object key (path inside the bucket).</param>
    /// <param name="verb">HTTP verb to allow (GET, PUT, etc.).</param>
    /// <returns> A pre-signed URL string, or null if generation failed.</returns>
    Task<Result<string>> GetPresignedUrlAsync(string key, HttpVerb verb);

    /// <summary>
    /// Deletes a file object from S3.
    /// </summary>
    /// <param name="key">The S3 key of the file.</param>
    Task<Result<bool>> DeleteFileAsync(string key);
}
