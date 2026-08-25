using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Cached read access to active <see cref="Domain.Entities.SystemSetting"/> rows.
/// </summary>
public interface ISystemSettingsProvider
{
    int GetInt(SystemSettingModule module, string key, int fallback);

    decimal GetDecimal(SystemSettingModule module, string key, decimal fallback);

    bool GetBool(SystemSettingModule module, string key, bool fallback);

    string GetString(SystemSettingModule module, string key, string fallback);
}

/// <summary>Invalidate cached settings after admin PATCH.</summary>
public interface ISystemSettingsCache
{
    Task RefreshAsync(CancellationToken ct = default);
}
