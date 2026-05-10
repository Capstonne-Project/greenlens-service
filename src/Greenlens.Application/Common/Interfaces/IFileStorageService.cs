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
    /// Delete a file from cloud storage by its key.
    /// </summary>
    Task DeleteAsync(string fileKey, CancellationToken ct = default);
}

public sealed record FileUploadResult(string Url, string Key);
