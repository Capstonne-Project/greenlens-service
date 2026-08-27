namespace Greenlens.Application.Common.Options;

/// <summary>
/// Meta Graph API settings for auto-posting community cleanup events to a Facebook Page.
/// Secrets (PageAccessToken, AppSecret) must come from env / user-secrets — never commit.
/// </summary>
public sealed class MetaPageOptions
{
    public const string SectionName = "Meta";

    public string AppId { get; init; } = "";

    public string AppSecret { get; init; } = "";

    public string PageId { get; init; } = "";

    public string PageAccessToken { get; init; } = "";

    public bool AutoPostEnabled { get; init; }

    /// <summary>When true, LEO can publish to Page via POST .../share/facebook-page.</summary>

    public string GraphApiVersion { get; init; } = "v21.0";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PageId) && !string.IsNullOrWhiteSpace(PageAccessToken);
}
