using System.Text.Json;

namespace Greenlens.Application.Features.Reports;

/// <summary>Parses persisted <see cref="Domain.Entities.Report.SuspiciousReasons"/> JSON for API responses.</summary>
public static class ReportSuspiciousReasonsParser
{
    public static IReadOnlyList<string> ParseCodes(string? suspiciousReasonsJson)
    {
        if (string.IsNullOrWhiteSpace(suspiciousReasonsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<string[]>(suspiciousReasonsJson) ?? [];
        }
        catch (JsonException)
        {
            return [suspiciousReasonsJson];
        }
    }

    /// <summary>Parses stored codes and maps to officer-facing messages (BR-REP-011).</summary>
    public static IReadOnlyList<string> ToDisplayMessages(string? suspiciousReasonsJson) =>
        ExifSuspicionEvaluator.ToDisplayMessages(ParseCodes(suspiciousReasonsJson));
}
