using System.Text.RegularExpressions;

namespace Greenlens.Application.Common;

/// <summary>
/// Strips common Markdown syntax for plain-text destinations (e.g. Facebook message field).
/// </summary>
internal static partial class MarkdownPlainText
{
    /// <summary>Markdown inline link — FE có thể render; Facebook post dùng <see cref="ToPlainPreservingLinks"/>.</summary>
    public static string FormatLink(string label, string url) =>
        $"[{label}]({url.TrimEnd('/')})";

    public static string ToPlain(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var text = markdown;

        text = ImageRegex().Replace(text, "$1");
        text = LinkRegex().Replace(text, "$1");
        text = BoldItalicRegex().Replace(text, "$1");
        text = InlineCodeRegex().Replace(text, "$1");
        text = HeadingRegex().Replace(text, "$1");
        text = BlockquoteRegex().Replace(text, "$1");
        text = ListMarkerRegex().Replace(text, "• ");
        text = HorizontalRuleRegex().Replace(text, string.Empty);

        return text.Trim();
    }

    /// <summary>
    /// Plain text cho Facebook/social: giữ URL sau label để client auto-linkify (không hỗ trợ anchor markdown).
    /// </summary>
    public static string ToPlainPreservingLinks(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var text = markdown;

        text = ImageRegex().Replace(text, "$1");
        text = LinkWithUrlRegex().Replace(text, "$1 ($2)");
        text = BoldItalicRegex().Replace(text, "$1");
        text = InlineCodeRegex().Replace(text, "$1");
        text = HeadingRegex().Replace(text, "$1");
        text = BlockquoteRegex().Replace(text, "$1");
        text = ListMarkerRegex().Replace(text, "• ");
        text = HorizontalRuleRegex().Replace(text, string.Empty);

        return text.Trim();
    }

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkWithUrlRegex();

    [GeneratedRegex(@"\*\*([^*]+)\*\*|\*([^*]+)\*|__([^_]+)__|_([^_]+)_", RegexOptions.Compiled)]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^>\s?", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s*[-*+]\s+", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ListMarkerRegex();

    [GeneratedRegex(@"^[-*_]{3,}\s*$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HorizontalRuleRegex();
}
