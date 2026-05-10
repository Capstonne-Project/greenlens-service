using Amazon.S3;
using Amazon.S3.Model;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Storage;

internal sealed class R2FileStorageService : IFileStorageService, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly R2Options _options;
    private readonly ILogger<R2FileStorageService> _logger;

    public R2FileStorageService(
        IOptions<R2Options> options,
        ILogger<R2FileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        _s3 = new AmazonS3Client(
            _options.AccessKeyId,
            _options.SecretAccessKey,
            config);
    }

    public async Task<FileUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default)
    {
        var key = $"{folder}/{Guid.NewGuid():N}_{fileName}";

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);

        var url = $"{_options.PublicUrl.TrimEnd('/')}/{key}";

        _logger.LogInformation("Uploaded file {Key} to R2 bucket {Bucket}", key, _options.BucketName);

        return new FileUploadResult(url, key);
    }

    public async Task DeleteAsync(string fileKey, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = fileKey
        };

        await _s3.DeleteObjectAsync(request, ct).ConfigureAwait(false);

        _logger.LogInformation("Deleted file {Key} from R2 bucket {Bucket}", fileKey, _options.BucketName);
    }

    public void Dispose() => _s3.Dispose();
}
