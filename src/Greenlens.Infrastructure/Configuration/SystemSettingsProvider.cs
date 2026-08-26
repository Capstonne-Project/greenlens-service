using System.Globalization;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Greenlens.Infrastructure.Configuration;

internal sealed class SystemSettingsProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemSettingsProvider> logger) : ISystemSettingsProvider, ISystemSettingsCache, IHostedService
{
    private volatile IReadOnlyDictionary<string, string> _snapshot =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public int GetInt(SystemSettingModule module, string key, int fallback)
    {
        var raw = GetRaw(module, key);
        return raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    public decimal GetDecimal(SystemSettingModule module, string key, decimal fallback)
    {
        var raw = GetRaw(module, key);
        return raw is not null && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    public bool GetBool(SystemSettingModule module, string key, bool fallback)
    {
        var raw = GetRaw(module, key);
        return raw is not null && bool.TryParse(raw, out var parsed)
            ? parsed
            : fallback;
    }

    public string GetString(SystemSettingModule module, string key, string fallback)
    {
        var raw = GetRaw(module, key);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw;
    }

    public Task RefreshAsync(CancellationToken ct = default) =>
        RefreshInternalAsync(ct);

    public Task StartAsync(CancellationToken cancellationToken) =>
        RefreshInternalAsync(cancellationToken);

    internal async Task RefreshInternalAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var rows = await db.Set<Domain.Entities.SystemSetting>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new { s.Module, s.Key, s.Value })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var dict = new Dictionary<string, string>(rows.Count, StringComparer.Ordinal);
        foreach (var row in rows)
            dict[BuildLookupKey(row.Module, row.Key)] = row.Value;

        _snapshot = dict;
        logger.LogInformation("System settings cache refreshed: {Count} active keys", dict.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private string? GetRaw(SystemSettingModule module, string key)
    {
        var snapshot = _snapshot;
        return snapshot.TryGetValue(BuildLookupKey(module, key), out var value) ? value : null;
    }

    private static string BuildLookupKey(SystemSettingModule module, string key)
        => $"{module}:{key}";
}
