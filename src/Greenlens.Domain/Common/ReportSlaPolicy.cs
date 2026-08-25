using Greenlens.Domain.Enums;

namespace Greenlens.Domain.Common;

/// <summary>Configurable SLA windows for pollution report verification and resolution.</summary>
public readonly record struct ReportSlaPolicy(
    int VerifyHours,
    int ResolveDaysCritical,
    int ResolveDaysHigh,
    int ResolveDaysMedium,
    int ResolveDaysLow)
{
    public static ReportSlaPolicy Default => new(24, 3, 5, 7, 10);

    public DateTime ComputeVerifyDueUtc(DateTime fromUtc) =>
        fromUtc.AddHours(VerifyHours);

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
