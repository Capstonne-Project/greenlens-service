using System.Text.RegularExpressions;

namespace Greenlens.Application.Features.Notifications;

/// <summary>Renders notification template strings with placeholder substitution.</summary>
internal static partial class NotificationTemplateRenderer
{
    // Supports canonical {report_code} and legacy admin/FE formats like {{ReportCode}}.
    [GeneratedRegex(@"\{\{?([a-zA-Z_][a-zA-Z0-9_]*)\}\}?", RegexOptions.Compiled)]
    private static partial Regex PlaceholderPattern();

    internal static string Render(string template, IReadOnlyDictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return PlaceholderPattern().Replace(template, match =>
        {
            var rawKey = match.Groups[1].Value;
            return TryResolveValue(data, rawKey, out var value) ? value : match.Value;
        });
    }

    internal static bool ContainsLegacyPlaceholders(string? text) =>
        !string.IsNullOrEmpty(text)
        && (text.Contains("{{", StringComparison.Ordinal)
            || PlaceholderPattern().Matches(text)
                .Any(m => !string.Equals(
                    NormalizePlaceholderKey(m.Groups[1].Value),
                    m.Groups[1].Value,
                    StringComparison.Ordinal)));

    private static bool TryResolveValue(
        IReadOnlyDictionary<string, string> data,
        string rawKey,
        out string value)
    {
        var normalized = NormalizePlaceholderKey(rawKey);

        if (data.TryGetValue(normalized, out value!))
            return true;

        foreach (var (key, candidate) in data)
        {
            if (string.Equals(key, normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizePlaceholderKey(key), normalized, StringComparison.Ordinal))
            {
                value = candidate;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static string NormalizePlaceholderKey(string key)
    {
        if (key.Contains('_', StringComparison.Ordinal))
            return key.ToLowerInvariant();

        var buffer = new System.Text.StringBuilder(key.Length + 4);
        for (var index = 0; index < key.Length; index++)
        {
            var ch = key[index];
            if (char.IsUpper(ch) && index > 0)
                buffer.Append('_');

            buffer.Append(char.ToLowerInvariant(ch));
        }

        return buffer.ToString();
    }
}
