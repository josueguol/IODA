using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using IODA.Core.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IODA.Core.Infrastructure.Storage;

/// <summary>
/// Storage provider para DigitalOcean Spaces (API compatible S3).
/// </summary>
public class DoSpacesMediaStorage : IMediaStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly string? _keyPrefix;
    private readonly ILogger<DoSpacesMediaStorage> _logger;

    public DoSpacesMediaStorage(IConfiguration configuration, ILogger<DoSpacesMediaStorage> logger)
    {
        _logger = logger;

        var endpoint = configuration["Media:DoSpaces:Endpoint"];
        var region = configuration["Media:DoSpaces:Region"] ?? "us-east-1";
        var accessKey = configuration["Media:DoSpaces:AccessKey"];
        var secretKey = configuration["Media:DoSpaces:SecretKey"];
        var bucket = configuration["Media:DoSpaces:Bucket"];
        var keyPrefix = configuration["Media:DoSpaces:KeyPrefix"];
        var usePathStyle = bool.TryParse(configuration["Media:DoSpaces:UsePathStyle"], out var parsed) && parsed;

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Media:DoSpaces:Endpoint is required when Media:Provider=do_spaces.");
        if (string.IsNullOrWhiteSpace(accessKey))
            throw new InvalidOperationException("Media:DoSpaces:AccessKey is required when Media:Provider=do_spaces.");
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Media:DoSpaces:SecretKey is required when Media:Provider=do_spaces.");
        if (string.IsNullOrWhiteSpace(bucket))
            throw new InvalidOperationException("Media:DoSpaces:Bucket is required when Media:Provider=do_spaces.");

        _bucket = bucket;
        _keyPrefix = string.IsNullOrWhiteSpace(keyPrefix) ? null : keyPrefix.Trim().Trim('/');

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var s3Config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            AuthenticationRegion = region,
            ForcePathStyle = usePathStyle,
        };

        _s3 = new AmazonS3Client(credentials, s3Config);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, Guid projectId, CancellationToken cancellationToken = default)
    {
        var safeName = SanitizeFileName(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var key = BuildKey($"{projectId:N}/{uniqueName}");

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            AutoCloseStream = false,
        };

        await _s3.PutObjectAsync(request, cancellationToken);
        _logger.LogDebug("Saved media to DigitalOcean Spaces bucket {Bucket}, key {Key}", _bucket, key);
        return key;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _s3.GetObjectAsync(_bucket, storageKey, cancellationToken);
        using (response)
        {
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            return ms;
        }
    }

    public async Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.GetObjectMetadataAsync(_bucket, storageKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        return _s3.DeleteObjectAsync(_bucket, storageKey, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        var request = new ListObjectsV2Request
        {
            BucketName = _bucket,
            Prefix = string.IsNullOrWhiteSpace(prefix) ? _keyPrefix : BuildKey(prefix),
            MaxKeys = 1000,
        };

        ListObjectsV2Response response;
        do
        {
            response = await _s3.ListObjectsV2Async(request, cancellationToken);
            foreach (var item in response.S3Objects)
                results.Add(item.Key);

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);

        return results;
    }

    private string BuildKey(string key)
    {
        var normalized = key.TrimStart('/');
        return string.IsNullOrWhiteSpace(_keyPrefix) ? normalized : $"{_keyPrefix}/{normalized}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(name) ? "file" : name;
    }
}
