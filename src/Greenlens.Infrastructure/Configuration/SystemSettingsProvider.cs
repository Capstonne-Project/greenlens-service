using System.Globalization;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Greenlens.Infrastructure.Configuration;

/// <summary>
/// Giữ bản sao system settings trong RAM để API đọc nhanh (check-in distance, EXIF threshold, …).
/// </summary>
/// <remarks>
/// Luồng đồng bộ (3 lớp — production):
/// 1. Startup → load DB vào <see cref="_snapshot"/>.
/// 2. Admin PATCH/Reset → <see cref="InvalidateAsync"/> refresh instance hiện tại + báo Redis pub/sub.
/// 3. Mỗi 60 giây → refresh lại từ DB (fallback nếu pub/sub lỡ hoặc sửa DB tay).
/// </remarks>
internal sealed class SystemSettingsProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemSettingsProvider> logger,
    IConnectionMultiplexer? redis)
    : ISystemSettingsProvider,
        ISystemSettingsCache,
        ISystemSettingsCacheInvalidator,
        IHostedService
{
    // Fallback an toàn: dù admin PATCH lỗi pub/sub, cache vẫn bắt kịp DB trong vòng 60 giây.
    private static readonly TimeSpan PeriodicRefreshInterval = TimeSpan.FromSeconds(60);

    // Bảng tra cứu in-memory: key = "Geo:check_in_max_distance_meters", value = "200".
    // volatile để thread-safe khi gán snapshot mới sau refresh.
    private volatile IReadOnlyDictionary<string, string> _snapshot =
        new Dictionary<string, string>(StringComparer.Ordinal);

    // Đánh dấu instance này đã subscribe Redis để unsubscribe khi shutdown.
    private bool _subscribedToInvalidateChannel;

    private CancellationTokenSource? _periodicRefreshCts;

    // ── Đọc setting (dùng bởi check-in, submit report, map, …) ──

    public int GetInt(SystemSettingModule module, string key, int fallback)
    {
        var raw = GetRaw(module, key);
        if (raw is null)
        {
            logger.LogDebug(
                "System setting {Module}:{Key} missing from cache; using fallback {Fallback}",
                module,
                key,
                fallback);
        }

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

    // ── Refresh chỉ trên instance hiện tại (không báo instance khác) ──

    public Task RefreshAsync(CancellationToken ct = default) =>
        RefreshInternalAsync(ct);

    // ── Admin PATCH/Reset gọi method này ──

    /// <summary>
    /// Bước 1: đọc lại DB → cập nhật RAM instance này.
    /// Bước 2: publish Redis để mọi replica production refresh (nếu có Redis).
    /// </summary>
    public async Task InvalidateAsync(CancellationToken ct = default)
    {
        // Luôn refresh local trước — request ngay sau PATCH trên cùng instance dùng giá trị mới.
        await RefreshInternalAsync(ct).ConfigureAwait(false);

        // Dev/local không cấu hình Redis → chỉ refresh 1 instance, đủ cho single-node.
        if (redis is null)
        {
            logger.LogWarning(
                "Redis not configured; system settings refreshed locally only (no cross-instance broadcast)");
            return;
        }

        // Broadcast tới channel chung; mỗi API instance đang subscribe sẽ tự refresh.
        var subscriberCount = await redis.GetSubscriber()
            .PublishAsync(RedisChannel.Literal(SystemSettingsCacheChannels.Invalidate), "refresh")
            .ConfigureAwait(false);

        logger.LogInformation(
            "Published system settings cache invalidation ({SubscriberCount} subscriber(s))",
            subscriberCount);
    }

    // ── IHostedService: chạy khi API khởi động ──

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Lần đầu load settings từ DB (hoặc sau deploy/restart).
        await RefreshInternalAsync(cancellationToken).ConfigureAwait(false);

        if (redis is not null)
        {
            // Lắng nghe tín hiệu từ admin PATCH trên cùng hoặc instance khác.
            var channel = RedisChannel.Literal(SystemSettingsCacheChannels.Invalidate);
            await redis.GetSubscriber()
                .SubscribeAsync(channel, (_, _) => OnRemoteInvalidateRequested())
                .ConfigureAwait(false);
            _subscribedToInvalidateChannel = true;

            logger.LogInformation(
                "Subscribed to system settings cache invalidation channel {Channel}",
                SystemSettingsCacheChannels.Invalidate);
        }
        else
        {
            logger.LogWarning(
                "Redis not configured; system settings cache uses startup + periodic DB refresh only");
        }

        // Fallback định kỳ — đảm bảo production luôn bắt kịp DB dù pub/sub hỏng.
        _periodicRefreshCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = RunPeriodicRefreshAsync(_periodicRefreshCts.Token);
    }

    /// <summary>Đọc toàn bộ setting active từ PostgreSQL và thay thế snapshot in-memory.</summary>
    internal async Task RefreshInternalAsync(CancellationToken ct, bool periodic = false)
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

        var previous = _snapshot;
        _snapshot = dict;

        if (periodic)
        {
            if (TryGetSnapshotValue(previous, SystemSettingModule.Geo, "check_in_max_distance_meters", out var oldCheckIn)
                && TryGetSnapshotValue(dict, SystemSettingModule.Geo, "check_in_max_distance_meters", out var newCheckIn)
                && !string.Equals(oldCheckIn, newCheckIn, StringComparison.Ordinal))
            {
                logger.LogInformation(
                    "Periodic refresh detected check_in_max_distance_meters change: {Old} → {New}",
                    oldCheckIn,
                    newCheckIn);
            }

            logger.LogDebug("System settings cache refreshed (periodic): {Count} active keys", dict.Count);
            return;
        }

        logger.LogInformation("System settings cache refreshed: {Count} active keys", dict.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _periodicRefreshCts?.Cancel();
        _periodicRefreshCts?.Dispose();
        _periodicRefreshCts = null;

        if (redis is null || !_subscribedToInvalidateChannel)
            return;

        await redis.GetSubscriber()
            .UnsubscribeAsync(RedisChannel.Literal(SystemSettingsCacheChannels.Invalidate))
            .ConfigureAwait(false);
        _subscribedToInvalidateChannel = false;
    }

    private async Task RunPeriodicRefreshAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PeriodicRefreshInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                await RefreshInternalAsync(ct, periodic: true).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected on shutdown.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Periodic system settings cache refresh loop stopped unexpectedly");
        }
    }

    // Callback Redis — không await trực tiếp trong handler pub/sub.
    private void OnRemoteInvalidateRequested() => _ = RefreshFromRemoteAsync();

    private async Task RefreshFromRemoteAsync()
    {
        try
        {
            await RefreshInternalAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh system settings cache after Redis invalidation");
        }
    }

    private string? GetRaw(SystemSettingModule module, string key)
    {
        var snapshot = _snapshot;
        return snapshot.TryGetValue(BuildLookupKey(module, key), out var value) ? value : null;
    }

    private static bool TryGetSnapshotValue(
        IReadOnlyDictionary<string, string> snapshot,
        SystemSettingModule module,
        string key,
        out string value)
    {
        if (snapshot.TryGetValue(BuildLookupKey(module, key), out var found) && found is not null)
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string BuildLookupKey(SystemSettingModule module, string key)
        => $"{module}:{key}";
}
