using System.Globalization;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Admin.SystemSettings;

internal static class SystemSettingValueValidator
{
    internal static bool TryValidate(
        SystemSettingValueType valueType,
        string rawValue,
        decimal? minValue,
        decimal? maxValue,
        out string? normalized,
        out string? errorMessage)
    {
        normalized = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            errorMessage = "Giá trị không được để trống.";
            return false;
        }

        normalized = rawValue.Trim();

        switch (valueType)
        {
            case SystemSettingValueType.Int:
                if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                {
                    errorMessage = "Giá trị phải là số nguyên.";
                    return false;
                }

                if (!IsInRange(intVal, minValue, maxValue, out errorMessage))
                    return false;

                normalized = intVal.ToString(CultureInfo.InvariantCulture);
                return true;

            case SystemSettingValueType.Decimal:
                if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var decVal))
                {
                    errorMessage = "Giá trị phải là số.";
                    return false;
                }

                if (!IsInRange(decVal, minValue, maxValue, out errorMessage))
                    return false;

                normalized = decVal.ToString(CultureInfo.InvariantCulture);
                return true;

            case SystemSettingValueType.Bool:
                if (!bool.TryParse(normalized, out var boolVal))
                {
                    errorMessage = "Giá trị phải là true hoặc false.";
                    return false;
                }

                normalized = boolVal.ToString().ToLowerInvariant();
                return true;

            case SystemSettingValueType.String:
            case SystemSettingValueType.Json:
                if (normalized.Length > 4000)
                {
                    errorMessage = "Giá trị quá dài (tối đa 4000 ký tự).";
                    return false;
                }

                if (valueType == SystemSettingValueType.Json
                    && !LooksLikeJson(normalized))
                {
                    errorMessage = "Giá trị JSON không hợp lệ.";
                    return false;
                }

                return true;

            default:
                errorMessage = "Kiểu giá trị không được hỗ trợ.";
                return false;
        }
    }

    private static bool IsInRange(decimal value, decimal? min, decimal? max, out string? errorMessage)
    {
        errorMessage = null;
        if (min.HasValue && value < min.Value)
        {
            errorMessage = $"Giá trị tối thiểu là {min.Value.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }

        if (max.HasValue && value > max.Value)
        {
            errorMessage = $"Giá trị tối đa là {max.Value.ToString(CultureInfo.InvariantCulture)}.";
            return false;
        }

        return true;
    }

    private static bool LooksLikeJson(string value)
    {
        var trimmed = value.Trim();
        return (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
               || (trimmed.StartsWith('[') && trimmed.EndsWith(']'));
    }
}
