using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.UnitTests.TestDoubles;

/// <summary>Returns seeded defaults matching current production hardcode.</summary>
internal sealed class DefaultSystemSettingsProvider : ISystemSettingsProvider
{
    public int GetInt(SystemSettingModule module, string key, int fallback) => fallback;

    public decimal GetDecimal(SystemSettingModule module, string key, decimal fallback) => fallback;

    public bool GetBool(SystemSettingModule module, string key, bool fallback) => fallback;

    public string GetString(SystemSettingModule module, string key, string fallback) => fallback;
}
