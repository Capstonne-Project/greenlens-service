namespace Greenlens.Application.Common;

/// <summary>
/// Normalizes and validates report image MIME types (BR-REP-001).
/// Clients (Swagger, iOS) often send non-standard MIME for HEIC — fall back to file extension.
/// </summary>
public static class ReportImageContentTypes
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
        [".heif"] = "image/heif",
    };

    private static readonly HashSet<string> UntrustedMimes = new(StringComparer.OrdinalIgnoreCase)
    {
        "",
        "application/octet-stream",
        "binary/octet-stream",
    };

    /// <summary>
    /// Resolves a trusted MIME type from declared content-type and/or file extension.
    /// </summary>
    public static bool TryResolve(string? fileName, string? contentType, out string normalizedMime)
    {
        normalizedMime = string.Empty;
        var mime = contentType?.Trim() ?? string.Empty;
        var normalizedFromMime = string.IsNullOrEmpty(mime) ? string.Empty : NormalizeMimeAlias(mime);

        // 1) Declared MIME maps to an allowed type (incl. heic-sequence → image/heic)
        if (!string.IsNullOrEmpty(normalizedFromMime) && Allowed.Contains(normalizedFromMime))
        {
            normalizedMime = normalizedFromMime.Equals("image/jpg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : normalizedFromMime;
            return true;
        }

        // 2) Extension fallback when client MIME is missing/untrusted/vendor-specific
        if (ShouldUseExtensionFallback(mime) && TryResolveFromExtension(fileName, out normalizedMime))
            return true;

        return false;
    }

    public static bool IsAllowed(string? fileName, string? contentType)
        => TryResolve(fileName, contentType, out _);

    private static bool ShouldUseExtensionFallback(string mime)
    {
        if (UntrustedMimes.Contains(mime))
            return true;

        // Explicit unsupported image/* (gif, bmp, …) — do not override via extension
        if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            || mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return false;

        // Vendor HEIC strings (application/vnd.apple.heic, …)
        return true;
    }

    private static bool TryResolveFromExtension(string? fileName, out string normalizedMime)
    {
        normalizedMime = string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || !ExtensionToMime.TryGetValue(ext, out var mapped))
            return false;

        normalizedMime = mapped;
        return true;
    }

    private static string NormalizeMimeAlias(string mime)
    {
        if (mime.Equals("image/heic-sequence", StringComparison.OrdinalIgnoreCase)
            || mime.Contains("heic", StringComparison.OrdinalIgnoreCase))
            return "image/heic";

        if (mime.Equals("image/heif-sequence", StringComparison.OrdinalIgnoreCase)
            || mime.Contains("heif", StringComparison.OrdinalIgnoreCase))
            return "image/heif";

        return mime;
    }
}
