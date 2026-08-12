using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Application.Features.Analytics.GetAdminReportFunnel;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Analytics.GetDeoReportFunnel;

public sealed class GetDeoReportFunnelQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<GetDeoReportFunnelQueryHandler> logger)
    : IRequestHandler<GetDeoReportFunnelQuery, Result<List<ReportFunnelStage>>>
{
    public async Task<Result<List<ReportFunnelStage>>> Handle(
        GetDeoReportFunnelQuery request, CancellationToken ct)
    {
        var scopeResult = await DepartmentContextResolver.ResolveAsync(users, currentUser, ct).ConfigureAwait(false);
        if (scopeResult.IsFailure)
            return scopeResult.Error!;

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var reportsInRange = DepartmentContextResolver
            .ApplyDepartmentScope(reports.QueryAsNoTracking(), scopeResult.Value.DepartmentId)
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        var submitted = await reportsInRange.CountAsync(ct).ConfigureAwait(false);
        var verified = await reportsInRange.CountAsync(r => r.VerifiedAt != null, ct).ConfigureAwait(false);
        var inProgress = await reportsInRange.CountAsync(r => r.StartedAt != null, ct).ConfigureAwait(false);
        var resolved = await reportsInRange.CountAsync(r => r.ResolvedAt != null, ct).ConfigureAwait(false);
        var closed = await reportsInRange.CountAsync(r => r.ClosedAt != null, ct).ConfigureAwait(false);

        logger.LogInformation("DEO report funnel for department {DepartmentId}", scopeResult.Value.DepartmentId);

        return new List<ReportFunnelStage>
        {
            new("Submitted", submitted),
            new("Verified", verified),
            new("InProgress", inProgress),
            new("Resolved", resolved),
            new("Closed", closed)
        };
    }
}
