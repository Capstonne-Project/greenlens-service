using System.Text.RegularExpressions;

namespace Greenlens.Application.Common;

/// <summary>
/// Strips common Markdown syntax for plain-text destinations (e.g. Facebook message field).
/// </summary>
internal static partial class MarkdownPlainText
{
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
        text = ListMarkerRegex().Replace(text, "- ");
        text = HorizontalRuleRegex().Replace(text, string.Empty);

        return text.Trim();
    }

    [GeneratedRegex(@"!\[([^\]]*)\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

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
