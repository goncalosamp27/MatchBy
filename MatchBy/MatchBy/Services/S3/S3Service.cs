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
    
    public async Task<Result<string>> UploadFormFileAsync(
        IFormFile file,
        string folder)
    {
        await using Stream stream = file.OpenReadStream();
        return await UploadFileAsync(stream, file.FileName, file.ContentType, folder);
    }
    
    public async Task<Result<string>> UploadBrowserFileAsync(
        IBrowserFile file,
        string folder)
    {
        await using Stream stream = file.OpenReadStream(maxAllowedSize: uploadOptions.Value.MaxFileSizeMegaBytes * 1024 * 1024);
        return await UploadFileAsync(stream, file.Name, file.ContentType, folder);
    }

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
