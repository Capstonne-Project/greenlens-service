namespace Greenlens.Infrastructure.Ai;

/// <summary>
/// Config for Google Gemini (free tier) — used only to auto-generate a short
/// report description from already-classified category/severity/subtypes.
/// Optional feature: when ApiKey is empty, description generation is skipped silently.
/// </summary>
public sealed class GeminiOptions
{
    public string ApiKey { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";

    public string Model { get; init; } = "gemini-3.1-flash-lite";

    public int TimeoutSeconds { get; init; } = 10;
}
