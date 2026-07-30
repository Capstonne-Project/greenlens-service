using FluentValidation;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogs;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x)
            .Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
            .WithMessage("fromDate must be before or equal to toDate.");
    }
}
