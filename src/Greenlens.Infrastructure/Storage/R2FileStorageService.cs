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
            ForcePathStyle = true,
            // Tránh GetObject treo vô hạn khi VPS không reach được R2 API.
            Timeout = TimeSpan.FromSeconds(30),
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
        var key = BuildObjectKey(folder, fileName);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3.PutObjectAsync(request, ct).ConfigureAwait(false);

        var url = BuildPublicUrl(key);

        _logger.LogInformation("Uploaded file {Key} to R2 bucket {Bucket}", key, _options.BucketName);

        return new FileUploadResult(url, key);
    }

    public Task<PresignedUploadResult> CreatePresignedUploadAsync(
        string fileName,
        string contentType,
        string folder,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = BuildObjectKey(folder, fileName);
        var expiresSeconds = Math.Clamp((int)expiresIn.TotalSeconds, 60, 3600);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(expiresSeconds),
            ContentType = contentType
        };

        // AWSSDK.S3 GetPreSignedURL is CPU-bound signing — wrap for interface async contract.
        var uploadUrl = _s3.GetPreSignedURL(request);
        var publicUrl = BuildPublicUrl(key);

        _logger.LogInformation(
            "Created R2 presigned PUT for {Key}, expiresIn={ExpiresInSeconds}s",
            key, expiresSeconds);

        return Task.FromResult(new PresignedUploadResult(
            uploadUrl,
            publicUrl,
            key,
            contentType,
            expiresSeconds));
    }

    public bool IsOwnedPublicUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!Uri.TryCreate(_options.PublicUrl.TrimEnd('/') + "/", UriKind.Absolute, out var publicBase))
            return false;

        return publicBase.IsBaseOf(uri);
    }

    public bool IsOwnedPublicUrl(string url, string key)
    {
        if (!IsOwnedPublicUrl(url))
            return false;

        return string.Equals(
            url.Trim(),
            BuildPublicUrl(key),
            StringComparison.Ordinal);
    }

    public string? TryGetKeyFromOwnedPublicUrl(string url)
    {
        if (!IsOwnedPublicUrl(url))
            return null;

        if (!Uri.TryCreate(_options.PublicUrl.TrimEnd('/') + "/", UriKind.Absolute, out var publicBase))
            return null;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return null;

        var relative = publicBase.MakeRelativeUri(uri).ToString();
        return string.IsNullOrWhiteSpace(relative)
            ? null
            : Uri.UnescapeDataString(relative.TrimEnd('/'));
    }

    public async Task<StoredFileDownload?> DownloadAsync(
        string key,
        long maxSizeBytes,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key) || maxSizeBytes <= 0)
            return null;

        try
        {
            _logger.LogDebug("R2 GetObject start | key={Key} maxBytes={MaxBytes}", key, maxSizeBytes);

            using var response = await _s3.GetObjectAsync(
                    new GetObjectRequest
                    {
                        BucketName = _options.BucketName,
                        Key = key.Trim()
                    },
                    ct)
                .ConfigureAwait(false);

            if (response.ContentLength <= 0 || response.ContentLength > maxSizeBytes)
            {
                _logger.LogWarning(
                    "R2 GetObject rejected size | key={Key} contentLength={ContentLength} maxBytes={MaxBytes}",
                    key,
                    response.ContentLength,
                    maxSizeBytes);
                return null;
            }

            using var buffer = new MemoryStream((int)response.ContentLength);
            await response.ResponseStream.CopyToAsync(buffer, ct).ConfigureAwait(false);

            if (buffer.Length <= 0 || buffer.Length > maxSizeBytes)
                return null;

            _logger.LogDebug(
                "R2 GetObject OK | key={Key} bytes={Bytes}",
                key,
                buffer.Length);

            return new StoredFileDownload(
                buffer.ToArray(),
                response.Headers.ContentType ?? "application/octet-stream",
                buffer.Length);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(
                ex,
                "R2 GetObject failed | key={Key} status={StatusCode} errorCode={ErrorCode}",
                key,
                ex.StatusCode,
                ex.ErrorCode);
            return null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("R2 GetObject cancelled | key={Key}", key);
            throw;
        }
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

    private string BuildPublicUrl(string key)
        => $"{_options.PublicUrl.TrimEnd('/')}/{key}";

    private static string BuildObjectKey(string folder, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "upload.bin";

        var cleanFolder = folder.Trim().Trim('/');
        return $"{cleanFolder}/{Guid.NewGuid():N}_{safeName}";
    }
}
