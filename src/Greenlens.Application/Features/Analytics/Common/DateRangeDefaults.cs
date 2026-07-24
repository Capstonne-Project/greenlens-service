namespace Greenlens.Application.Features.Analytics.Common;

/// <summary>Resolves optional from/to dashboard query params to a concrete UTC range.</summary>
public static class DateRangeDefaults
{
    public const int DefaultLookbackDays = 30;

    public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime utcNow)
    {
        var resolvedTo = (to ?? utcNow).ToUniversalTime();
        var resolvedFrom = (from ?? resolvedTo.AddDays(-DefaultLookbackDays)).ToUniversalTime();
        return (resolvedFrom, resolvedTo);
    }
}
