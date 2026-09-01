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
/// Luồng đồng bộ:
/// 1. Startup → load DB vào <see cref="_snapshot"/>.
/// 2. Admin PATCH/Reset → <see cref="InvalidateAsync"/> refresh instance hiện tại + báo Redis.
/// 3. Các instance production khác nhận Redis → <see cref="RefreshInternalAsync"/> lại từ DB.
/// Không cần restart API sau khi admin đổi setting (miễn là có Redis trên production).
/// </remarks>
internal sealed class SystemSettingsProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<SystemSettingsProvider> logger,
    IConnectionMultiplexer? redis = null)
    : ISystemSettingsProvider,
        ISystemSettingsCache,
        ISystemSettingsCacheInvalidator,
        IHostedService
{
    // Bảng tra cứu in-memory: key = "Geo:check_in_max_distance_meters", value = "200".
    // volatile để thread-safe khi gán snapshot mới sau refresh.
    private volatile IReadOnlyDictionary<string, string> _snapshot =
        new Dictionary<string, string>(StringComparer.Ordinal);

    // Đánh dấu instance này đã subscribe Redis để unsubscribe khi shutdown.
    private bool _subscribedToInvalidateChannel;

    // ── Đọc setting (dùng bởi check-in, submit report, map, …) ──

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
            return;

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

        if (redis is null)
            return;

        // Lắng nghe tín hiệu từ instance khác (admin PATCH trên replica B → replica A cũng refresh).
        var channel = RedisChannel.Literal(SystemSettingsCacheChannels.Invalidate);
        await redis.GetSubscriber()
            .SubscribeAsync(channel, (_, _) => OnRemoteInvalidateRequested())
            .ConfigureAwait(false);
        _subscribedToInvalidateChannel = true;

        logger.LogInformation(
            "Subscribed to system settings cache invalidation channel {Channel}",
            SystemSettingsCacheChannels.Invalidate);
    }

    /// <summary>Đọc toàn bộ setting active từ PostgreSQL và thay thế snapshot in-memory.</summary>
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (redis is null || !_subscribedToInvalidateChannel)
            return;

        await redis.GetSubscriber()
            .UnsubscribeAsync(RedisChannel.Literal(SystemSettingsCacheChannels.Invalidate))
            .ConfigureAwait(false);
        _subscribedToInvalidateChannel = false;
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

    private static string BuildLookupKey(SystemSettingModule module, string key)
        => $"{module}:{key}";
}
