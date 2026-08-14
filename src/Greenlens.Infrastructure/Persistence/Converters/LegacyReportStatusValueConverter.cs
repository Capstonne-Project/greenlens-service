using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Greenlens.Infrastructure.Persistence.Converters;

/// <summary>
/// Maps removed v2 report statuses (Dispatched, Assigned, …) when reading legacy rows.
/// </summary>
/// <remarks>See report-workflow-v1.3-direct-dispatch.md §9.</remarks>
internal sealed class LegacyReportStatusValueConverter()
    : ValueConverter<ReportStatus, string>(
        v => v.ToString(),
        v => ParseRequired(v))
{
    internal static ReportStatus ParseRequired(string value) =>
        ParseOptional(value) ?? throw new InvalidOperationException(
            $"Unknown report status value '{value}'.");

    internal static ReportStatus? ParseOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (LegacyMap.TryGetValue(value, out var mapped))
            return mapped;

        return Enum.TryParse<ReportStatus>(value, ignoreCase: false, out var parsed)
            ? parsed
            : null;
    }

    private static readonly Dictionary<string, ReportStatus> LegacyMap =
        new(StringComparer.Ordinal)
        {
            ["Dispatched"] = ReportStatus.InProgress,
            ["Assigned"] = ReportStatus.InProgress,
            ["PenaltyIssued"] = ReportStatus.InProgress,
            ["ClosedNoViolation"] = ReportStatus.Closed
        };
}

internal sealed class LegacyNullableReportStatusValueConverter()
    : ValueConverter<ReportStatus?, string?>(
        v => v.HasValue ? v.Value.ToString() : null,
        v => LegacyReportStatusValueConverter.ParseOptional(v));
