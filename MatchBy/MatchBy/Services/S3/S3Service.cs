using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MatchBy.Models;
using MatchBy.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;

namespace MatchBy.Services.S3;

public class S3Service(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings, ILogger<S3Service> logger, IOptions<UploadSettings> uploadOptions) : IS3Service
{
    /// <summary>
    /// Uploads a file stream to S3 storage with a generated unique key.
    /// </summary>
    /// <param name="stream">The file stream to upload.</param>
    /// <param name="fileName">The original file name (used for metadata and extension extraction).</param>
    /// <param name="contentType">The MIME content type of the file.</param>
    /// <param name="folder">The S3 folder path where the file should be stored.</param>
    /// <returns>
    /// A result containing the generated file key if successful, or an error message if the upload fails.
    /// </returns>
    /// <remarks>
    /// The file key is generated using a GUID v7 and the original file extension.
    /// The original file name is stored in the S3 object metadata.
    /// </remarks>
    private async Task<Result<string>> UploadFileAsync(
        Stream stream,
        string fileName, 
        string contentType,
        string folder)
    {
        try
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            string key = $"{Guid.CreateVersion7()}{ext}";

            var request = new PutObjectRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"{folder}/{key}",
                InputStream = stream,
                ContentType = contentType,
                Metadata =
                {
                    ["file-name"] = fileName
                }
            };

            PutObjectResponse? response = await s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("File '{File}' uploaded successfully to bucket '{Bucket}' as '{Key}'.",
                    fileName, s3Settings.Value.BucketName, key);
                return Result<string>.Ok(key);
            }

            logger.LogWarning("File upload failed for '{File}' (bucket '{Bucket}'). Status code: {Status}",
                fileName, s3Settings.Value.BucketName, response.HttpStatusCode);
            return Result<string>.Fail("File upload failed.");
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while uploading '{File}'.", fileName);
            return Result<string>.Fail("File upload failed due to AWS S3 error.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while uploading '{File}'.", fileName);
            return Result<string>.Fail("Unexpected error.");
        }
    }
    
    /// <summary>
    /// Uploads an IFormFile to S3 storage.
    /// </summary>
    /// <param name="file">The form file to upload.</param>
    /// <param name="folder">The S3 folder path where the file should be stored.</param>
    /// <returns>
    /// A result containing the generated file key if successful, or an error message if the upload fails.
    /// </returns>
    public async Task<Result<string>> UploadFormFileAsync(
        IFormFile file,
        string folder)
    {
        await using Stream stream = file.OpenReadStream();
        return await UploadFileAsync(stream, file.FileName, file.ContentType, folder);
    }
    
    /// <summary>
    /// Uploads an IBrowserFile to S3 storage with size validation.
    /// </summary>
    /// <param name="file">The browser file to upload.</param>
    /// <param name="folder">The S3 folder path where the file should be stored.</param>
    /// <returns>
    /// A result containing the generated file key if successful, or an error message if the upload fails.
    /// </returns>
    /// <remarks>
    /// The file size is validated against the configured maximum file size limit before upload.
    /// </remarks>
    public async Task<Result<string>> UploadBrowserFileAsync(
        IBrowserFile file,
        string folder)
    {
        await using Stream stream = file.OpenReadStream(maxAllowedSize: uploadOptions.Value.MaxFileSizeMegaBytes * 1024 * 1024);
        return await UploadFileAsync(stream, file.Name, file.ContentType, folder);
    }

    /// <summary>
    /// Generates a presigned URL for accessing an S3 object.
    /// </summary>
    /// <param name="key">The S3 object key (full path including folder).</param>
    /// <param name="verb">The HTTP verb (GET, PUT, etc.) for the presigned URL.</param>
    /// <returns>
    /// A result containing the presigned URL if successful, or an error message if generation fails.
    /// </returns>
    /// <remarks>
    /// The presigned URL expires after the configured default expiry time (typically 30 minutes).
    /// </remarks>
    public async Task<Result<string>> GetPresignedUrlAsync(string key, HttpVerb verb)
    {
        try
        {
            var expiresIn = TimeSpan.FromMinutes(s3Settings.Value.DefaultUrlExpiry);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = key,
                Verb = verb,
                Expires = DateTime.UtcNow.Add(expiresIn)
            };

            string? url = await s3Client.GetPreSignedURLAsync(request);
            logger.LogInformation("Generated pre-signed URL for '{Key}' (expires in {Minutes} minutes).",
                key, expiresIn.TotalMinutes);
            return Result<string>.Ok(url);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while generating pre-signed URL for '{Key}'.", key);
            return Result<string>.Fail("AWS S3 error.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while generating pre-signed URL for '{Key}'.", key);
            return Result<string>.Fail("Unexpected error.");
        }
    }

    /// <summary>
    /// Deletes a file from S3 storage.
    /// </summary>
    /// <param name="key">The S3 object key (full path including folder) to delete.</param>
    /// <returns>
    /// A result containing true if the file was successfully deleted, or an error message if deletion fails.
    /// </returns>
    public async Task<Result<bool>> DeleteFileAsync(string key)
    {
        try
        {
            DeleteObjectResponse? response = await s3Client.DeleteObjectAsync(
                bucketName: s3Settings.Value.BucketName,
                key: key);

            if (response.HttpStatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK)
            {
                logger.LogInformation("File '{Key}' deleted successfully from bucket '{Bucket}'.",
                    key, s3Settings.Value.BucketName);
                return Result<bool>.Ok(true);
            }

            logger.LogWarning("Failed to delete '{Key}' from bucket '{Bucket}'. Status code: {Status}",
                key, s3Settings.Value.BucketName, response.HttpStatusCode);
            return Result<bool>.Fail("File deletion failed.");
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while deleting file '{Key}'.", key);
            return Result<bool>.Fail("AWS S3 error.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while deleting file '{Key}'.", key);
            return Result<bool>.Fail("Unexpected error.");
        }
    }
}
