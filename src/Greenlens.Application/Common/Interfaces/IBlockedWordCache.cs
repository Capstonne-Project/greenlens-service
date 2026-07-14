namespace Greenlens.Application.Common.Interfaces;

/// <summary>In-memory snapshot of active blocked words for profanity filtering.</summary>
public interface IBlockedWordCache
{
    IReadOnlyList<string> GetActiveWords();

    Task RefreshAsync(CancellationToken cancellationToken);
}
