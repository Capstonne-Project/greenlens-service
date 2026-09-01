using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Đọc system settings từ cache RAM (không query DB mỗi request).
/// Handler check-in, submit report, map, … inject interface này.
/// </summary>
public interface ISystemSettingsProvider
{
    int GetInt(SystemSettingModule module, string key, int fallback);

    decimal GetDecimal(SystemSettingModule module, string key, decimal fallback);

    bool GetBool(SystemSettingModule module, string key, bool fallback);

    string GetString(SystemSettingModule module, string key, string fallback);
}

/// <summary>
/// Refresh cache trên đúng 1 API instance (không thông báo instance khác).
/// Thường không gọi trực tiếp từ handler — dùng <see cref="ISystemSettingsCacheInvalidator"/> sau admin PATCH.
/// </summary>
public interface ISystemSettingsCache
{
    Task RefreshAsync(CancellationToken ct = default);
}

/// <summary>
/// Gọi sau admin PATCH hoặc Reset module (thường qua <see cref="ISystemSettingsCacheInvalidationCollector.Schedule"/>).
/// Refresh instance hiện tại + publish Redis để mọi replica production cập nhật ngay, không cần restart.
/// </summary>
public interface ISystemSettingsCacheInvalidator
{
    Task InvalidateAsync(CancellationToken ct = default);
}
