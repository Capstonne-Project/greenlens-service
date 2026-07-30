using FluentValidation;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogStats;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogStatsQueryValidator : AbstractValidator<GetAuditLogStatsQuery>
{
    private const int MaxRangeDays = 90;

    public GetAuditLogStatsQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.FromDate <= x.ToDate)
            .WithMessage("fromDate must be before or equal to toDate.");
        RuleFor(x => x)
            .Must(x => (x.ToDate - x.FromDate).TotalDays <= MaxRangeDays)
            .WithMessage($"Date range must not exceed {MaxRangeDays} days.");
    }
}
