namespace Greenlens.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to cloud storage and return the public URL + storage key.
    /// </summary>
    Task<FileUploadResult> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    /// <summary>
    /// Create a short-lived presigned PUT URL so the client can upload directly to R2.
    /// Client MUST send the exact <paramref name="contentType"/> header when PUTting.
    /// </summary>
    Task<PresignedUploadResult> CreatePresignedUploadAsync(
        string fileName,
        string contentType,
        string folder,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    /// <summary>
    /// Download a private object by key for trusted server-side processing.
    /// Returns null when the object does not exist or exceeds <paramref name="maxSizeBytes"/>.
    /// </summary>
    Task<StoredFileDownload?> DownloadAsync(
        string key,
        long maxSizeBytes,
        CancellationToken ct = default);

    /// <summary>
    /// Returns true when <paramref name="url"/> is an absolute HTTPS URL under the configured public CDN base.
    /// </summary>
    bool IsOwnedPublicUrl(string url);

    /// <summary>
    /// Returns true when the public URL belongs to this storage and exactly maps to <paramref name="key"/>.
    /// </summary>
    bool IsOwnedPublicUrl(string url, string key);

    /// <summary>
    /// Delete a file from cloud storage by its key.
    /// </summary>
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
}

public sealed record FileUploadResult(string Url, string Key);

public sealed record PresignedUploadResult(
    string UploadUrl,
    string PublicUrl,
    string Key,
    string ContentType,
    int ExpiresInSeconds);

public sealed record StoredFileDownload(
    byte[] Bytes,
    string ContentType,
    long SizeBytes);
