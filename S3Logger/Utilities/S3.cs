using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace Zooscape.Infrastructure.S3Logger.Utilities;

public static class S3
{
    public static string LogDirectory { get; set; } = string.Empty;

    public static async Task UploadLogs()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PUSH_LOGS_TO_S3")))
        {
            return;
        }

        var s3Bucket = Environment.GetEnvironmentVariable("S3_BUCKET_NAME");
        if (string.IsNullOrWhiteSpace(s3Bucket))
            throw new Exception("No S3_BUCKET_NAME environment variable defined.");

        string[] parts = s3Bucket.Split('/');
        string bucketName = parts[0];
        string prefix = "/" + string.Join("/", parts.Skip(1));
        RegionEndpoint? bucketRegion = RegionEndpoint.GetBySystemName(
            Environment.GetEnvironmentVariable("AWS_REGION")
        );

        AmazonS3Client s3Client = new(bucketRegion);
        TransferUtility transferUtility = new(s3Client);
        TransferUtilityUploadDirectoryRequest uploadRequest = new()
        {
            BucketName = bucketName,
            Directory = LogDirectory,
            KeyPrefix = prefix,
        };
        await transferUtility.UploadDirectoryAsync(uploadRequest);
    }
}
