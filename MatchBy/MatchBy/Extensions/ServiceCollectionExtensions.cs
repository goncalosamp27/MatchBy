using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using MatchBy.Services;
using MatchBy.Settings;
using Microsoft.Extensions.Options;

namespace MatchBy.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAwsS3(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<S3Settings>(config.GetSection("S3Settings"));
        services.AddSingleton<IAmazonS3>(sp =>
        {
            S3Settings s3 = sp.GetRequiredService<IOptions<S3Settings>>().Value;
            var creds = new BasicAWSCredentials(s3.AccessKey, s3.SecretKey);
            var cfg = new AmazonS3Config { RegionEndpoint = RegionEndpoint.GetBySystemName(s3.Region) };
            return new AmazonS3Client(creds, cfg);
        });
        services.AddSingleton<IS3Service, S3Service>();
        return services;
    }
}
