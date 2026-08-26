using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// File-system temp store for images between Analyze and Submit steps.
/// TTL from system_settings (ai_temp_image_ttl_seconds). Each entry is stored as two files:
///   {tempId}.bin   — raw image bytes
///   {tempId}.meta  — JSON metadata (fileName, contentType, expiresAt)
/// </summary>
internal sealed class TempImageStore(
    ISystemSettingsProvider systemSettings,
    ILogger<TempImageStore> logger) : ITempImageStore
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "greenlens_temp_images");

    public async Task<string> SaveAsync(
        byte[] imageBytes,
        string fileName,
        string contentType,
        AiClassificationResult? aiResult = null,
        string? publicUrl = null,
        string? storageKey = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(_folder);

        var tempId = Guid.NewGuid().ToString("N");
        var binPath = BinPath(tempId);
        var metaPath = MetaPath(tempId);

        await File.WriteAllBytesAsync(binPath, imageBytes, ct).ConfigureAwait(false);

        var ttl = ResolveTtl();
        var meta = new TempMeta(
            fileName,
            contentType,
            DateTime.UtcNow.Add(ttl),
            aiResult,
            publicUrl,
            storageKey);
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), ct).ConfigureAwait(false);

        logger.LogDebug("Saved temp image {TempId} expires {Expires}", tempId, meta.ExpiresAt);
        return tempId;
    }

    public async Task<TempImageEntry?> GetAsync(string tempId, CancellationToken ct = default)
    {
        var binPath = BinPath(tempId);
        var metaPath = MetaPath(tempId);

        if (!File.Exists(binPath) || !File.Exists(metaPath))
            return null;

        var metaJson = await File.ReadAllTextAsync(metaPath, ct).ConfigureAwait(false);
        var meta = JsonSerializer.Deserialize<TempMeta>(metaJson);
        if (meta is null) return null;

        if (DateTime.UtcNow > meta.ExpiresAt)
        {
            // expired — clean up silently
            TryDelete(binPath);
            TryDelete(metaPath);
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(binPath, ct).ConfigureAwait(false);
        return new TempImageEntry(
            bytes,
            meta.FileName,
            meta.ContentType,
            meta.ExpiresAt,
            meta.AiResult,
            meta.PublicUrl,
            meta.StorageKey);
    }

    public Task DeleteAsync(string tempId, CancellationToken ct = default)
    {
        TryDelete(BinPath(tempId));
        TryDelete(MetaPath(tempId));
        return Task.CompletedTask;
    }

    private TimeSpan ResolveTtl()
    {
        var seconds = ModuleSystemSettings.Ai(systemSettings).TempImageTtlSeconds;
        return TimeSpan.FromSeconds(Math.Max(60, seconds));
    }

    private string BinPath(string tempId) => Path.Combine(_folder, $"{tempId}.bin");
    private string MetaPath(string tempId) => Path.Combine(_folder, $"{tempId}.meta");

    private void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to delete temp file {Path}", path); }
    }

    private sealed record TempMeta(
        string FileName,
        string ContentType,
        DateTime ExpiresAt,
        AiClassificationResult? AiResult,
        string? PublicUrl,
        string? StorageKey);
}
