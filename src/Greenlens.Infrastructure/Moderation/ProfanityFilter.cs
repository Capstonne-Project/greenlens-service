using Greenlens.Application.Common.Interfaces;

namespace Greenlens.Infrastructure.Moderation;

public sealed class ProfanityFilter(IBlockedWordCache cache) : IProfanityFilter
{
    public bool ContainsProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.ToLowerInvariant();
        foreach (var word in cache.GetActiveWords())
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            if (normalized.Contains(word, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
