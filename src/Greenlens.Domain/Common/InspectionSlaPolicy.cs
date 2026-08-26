using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Common;

/// <summary>Configurable SLA windows for inspection dossier resolution.</summary>
public readonly record struct InspectionSlaPolicy(
    int ResolveDaysCritical,
    int ResolveDaysHigh,
    int ResolveDaysMedium,
    int ResolveDaysLow)
{
    public static InspectionSlaPolicy Default => new(3, 5, 7, 10);

    public DateTime ComputeResolveDueUtc(Severity severity, DateTime fromUtc) =>
        fromUtc.AddDays(severity switch
        {
            Severity.Critical => ResolveDaysCritical,
            Severity.High => ResolveDaysHigh,
            Severity.Medium => ResolveDaysMedium,
            Severity.Low => ResolveDaysLow,
            _ => ResolveDaysMedium
        });
}
