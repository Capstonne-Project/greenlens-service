using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Entities;

/// <summary>
/// Admin-configurable business threshold stored as key/value per module.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class SystemSetting : AuditableEntity
{
    private SystemSetting() { }

    public SystemSettingModule Module { get; private set; }

    /// <summary>snake_case unique within <see cref="Module"/>.</summary>
    public string Key { get; private set; } = default!;

    public SystemSettingValueType ValueType { get; private set; }

    public string Value { get; private set; } = default!;

    public string DefaultValue { get; private set; } = default!;

    public string Description { get; private set; } = default!;

    public decimal? MinValue { get; private set; }

    public decimal? MaxValue { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static SystemSetting Create(
        SystemSettingModule module,
        string key,
        SystemSettingValueType valueType,
        string value,
        string description,
        decimal? minValue = null,
        decimal? maxValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new SystemSetting
        {
            Module = module,
            Key = key.Trim(),
            ValueType = valueType,
            Value = value.Trim(),
            DefaultValue = value.Trim(),
            Description = description.Trim(),
            MinValue = minValue,
            MaxValue = maxValue,
            IsActive = true
        };
    }

    public void UpdateValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public void ResetToDefault() => Value = DefaultValue;

    public void SetActive(bool isActive) => IsActive = isActive;
}
