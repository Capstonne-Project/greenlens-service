using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Mappings;

/// <summary>Maps AI severity strings to domain <see cref="Severity"/>.</summary>
public static class AiSeverityMapper
{
    public static Severity Parse(string? severity) =>
        severity?.Trim().ToUpperInvariant() switch
        {
            "LOW" => Severity.Low,
            "HIGH" => Severity.High,
            "CRITICAL" => Severity.Critical,
            _ => Severity.Medium
        };
}
