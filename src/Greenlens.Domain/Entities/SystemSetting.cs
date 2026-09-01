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

    /// <summary>Short Vietnamese label for admin UI.</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Display unit for admin input (e.g. m, MB, ngày). Null = no suffix.</summary>
    public string? Unit { get; private set; }

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
        string title,
        string description,
        string? unit = null,
        decimal? minValue = null,
        decimal? maxValue = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new SystemSetting
        {
            Module = module,
            Key = key.Trim(),
            Title = title.Trim(),
            Unit = NormalizeUnit(unit),
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

    public void UpdateMetadata(string title, string description, string? unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Title = title.Trim();
        Description = description.Trim();
        Unit = NormalizeUnit(unit);
    }

    public void UpdateBounds(decimal? minValue, decimal? maxValue)
    {
        MinValue = minValue;
        MaxValue = maxValue;
    }

    private static string? NormalizeUnit(string? unit) =>
        string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();

    public void ResetToDefault() => Value = DefaultValue;

    public void SetActive(bool isActive) => IsActive = isActive;
}
