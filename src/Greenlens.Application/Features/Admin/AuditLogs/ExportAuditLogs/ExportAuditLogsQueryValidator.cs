using FluentValidation;

namespace Greenlens.Application.Features.Admin.AuditLogs.ExportAuditLogs;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class ExportAuditLogsQueryValidator : AbstractValidator<ExportAuditLogsQuery>
{
    private const int MaxRangeDays = 90;

    public ExportAuditLogsQueryValidator()
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
