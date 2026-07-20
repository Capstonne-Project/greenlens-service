using Greenlens.Domain.Common;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Admin-managed profanity word/phrase for content moderation.
/// </summary>
/// <remarks>
/// Implements: BR-REP-004 (report description), BR-CMT-003 (comments), BR-ADM-010 (audit on changes).
/// </remarks>
public sealed class BlockedWord : AuditableEntity
{
    private BlockedWord() { }

    /// <summary>Normalized lowercase word or phrase (unique).</summary>
    public string Word { get; private set; } = default!;

    /// <summary>Optional admin note (e.g. why this word was added).</summary>
    public string? Note { get; private set; }

    /// <summary>Inactive words are not applied by the profanity filter.</summary>
    public bool IsActive { get; private set; } = true;

    public static BlockedWord Create(string word, string? note = null)
    {
        var normalized = NormalizeWord(word);
        return new BlockedWord
        {
            Word = normalized,
            Note = note?.Trim(),
            IsActive = true
        };
    }

    public void Update(string word, string? note, bool isActive)
    {
        Word = NormalizeWord(word);
        Note = note?.Trim();
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;

    public static string NormalizeWord(string word) =>
        word.Trim().ToLowerInvariant();
}
