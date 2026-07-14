using Greenlens.Application.Common.Interfaces;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Moderation;

/// <summary>
/// Loads active blocked words from DB into memory. Refreshed on startup and after admin CRUD.
/// </summary>
public sealed class BlockedWordCache(
    IServiceScopeFactory scopeFactory,
    ILogger<BlockedWordCache> logger) : IBlockedWordCache, IHostedService
{
    private volatile string[] _activeWords = [];

    public IReadOnlyList<string> GetActiveWords() => _activeWords;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var words = await db.BlockedWords.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Word)
            .Select(w => w.Word)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _activeWords = words.ToArray();
        logger.LogInformation("Blocked word cache refreshed: {Count} active words", _activeWords.Length);
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        RefreshAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
