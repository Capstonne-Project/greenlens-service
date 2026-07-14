using Greenlens.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Greenlens.Infrastructure.Moderation;

public sealed class ModerationOptions
{
    public const string SectionName = "Moderation";

    /// <summary>Blocked words/phrases (case-insensitive substring match). BR-CMT-003 phase 1.</summary>
    public string[] BlockedWords { get; init; } =
    [
        "địt", "đụ", "lồn", "cặc", "đéo", "vcl", "vl", "fuck", "shit", "bitch"
    ];
}

public sealed class ProfanityFilter(IOptions<ModerationOptions> options) : IProfanityFilter
{
    public bool ContainsProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.ToLowerInvariant();
        foreach (var word in options.Value.BlockedWords)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            if (normalized.Contains(word.Trim().ToLowerInvariant(), StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
