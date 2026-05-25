namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Temporary storage for uploaded images between the Analyze and Submit steps.
/// TTL = 15 minutes (BR-AI flow, Step 1 → Step 2).
/// </summary>
public interface ITempImageStore
{
    /// <summary>Save bytes to temp store and return the assigned temp ID.</summary>
    Task<string> SaveAsync(byte[] imageBytes, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Retrieve a temp image. Returns null when not found or expired.</summary>
    Task<TempImageEntry?> GetAsync(string tempId, CancellationToken ct = default);

    /// <summary>Delete a temp image after successful submit.</summary>
    Task DeleteAsync(string tempId, CancellationToken ct = default);
}

public sealed record TempImageEntry(
    byte[] Bytes,
    string FileName,
    string ContentType,
    DateTime ExpiresAt);
