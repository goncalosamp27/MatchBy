using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using MatchBy.Settings;
using Microsoft.Extensions.Options;

namespace MatchBy.Services;

public class S3Service(IAmazonS3 s3Client, IOptions<S3Settings> s3Settings, ILogger<S3Service> logger) : IS3Service
{
    public async Task<string?> UploadFileAsync(
        IFormFile file,
        string folder)
    {
        try
        {
            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string key = $"{Guid.CreateVersion7()}{ext}";
            await using Stream stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = s3Settings.Value.BucketName,
                Key = $"{folder}/{key}",
                InputStream = stream,
                ContentType = file.ContentType,
                Metadata =
                {
                    ["file-name"] = file.FileName
                }
            };

            PutObjectResponse? response = await s3Client.PutObjectAsync(request);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                logger.LogInformation("File '{File}' uploaded successfully to bucket '{Bucket}' as '{Key}'.",
                    file.FileName, s3Settings.Value.BucketName, key);
                return key;
            }

            logger.LogWarning("File upload failed for '{File}' (bucket '{Bucket}'). Status code: {Status}",
                file.FileName, s3Settings.Value.BucketName, response.HttpStatusCode);
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while uploading '{File}'.", file.FileName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while uploading '{File}'.", file.FileName);
            return null;
        }
    }

    public async Task<string?> GetPresignedUrlAsync(string key, HttpVerb verb)
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
            return url;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while generating pre-signed URL for '{Key}'.", key);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while generating pre-signed URL for '{Key}'.", key);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            DeleteObjectResponse? response = await s3Client.DeleteObjectAsync(
                bucketName: s3Settings.Value.BucketName,
                key: key);

            if (response.HttpStatusCode == HttpStatusCode.NoContent)
            {
                logger.LogInformation("File '{Key}' deleted successfully from bucket '{Bucket}'.",
                    key, s3Settings.Value.BucketName);
                return true;
            }

            logger.LogWarning("Failed to delete '{Key}' from bucket '{Bucket}'. Status code: {Status}",
                key, s3Settings.Value.BucketName, response.HttpStatusCode);
            return false;
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "AWS S3 error while deleting file '{Key}'.", key);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while deleting file '{Key}'.", key);
            return false;
        }
    }
}
