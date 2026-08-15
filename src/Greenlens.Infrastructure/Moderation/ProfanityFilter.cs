using System.Text.RegularExpressions;
using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Moderation;

/// <summary>
/// Whole-token profanity check — blocked words must appear as standalone tokens,
/// not as substrings (e.g. "đụ" must not match "nước đục").
/// </summary>
/// <remarks>Implements: BR-REP-004, BR-CMT-003.</remarks>
public sealed class ProfanityFilter(IBlockedWordCache cache) : IProfanityFilter
{
    private static readonly Regex TokenSplitRegex = new(
        @"[^\p{L}]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool ContainsProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var tokens = Tokenize(text);
        foreach (var word in cache.GetActiveWords())
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            if (tokens.Contains(word))
                return true;
        }

        return false;
    }

    private static HashSet<string> Tokenize(string text)
    {
        var normalized = text.ToLowerInvariant();
        var parts = TokenSplitRegex.Split(normalized);
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (part.Length > 0)
                tokens.Add(part);
        }

        return tokens;
    }
}
