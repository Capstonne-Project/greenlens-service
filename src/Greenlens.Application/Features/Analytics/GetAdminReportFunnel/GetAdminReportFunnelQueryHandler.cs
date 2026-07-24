using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Analytics.GetAdminReportFunnel;

/// <summary>
/// Lifecycle funnel: how many reports (created in range) reached each stage at least once.
/// Stages are cumulative — e.g. "Resolved" includes reports that are now Closed.
/// </summary>
public sealed class GetAdminReportFunnelQueryHandler(
    IReportRepository reports,
    IDateTimeProvider clock,
    ILogger<GetAdminReportFunnelQueryHandler> logger)
    : IRequestHandler<GetAdminReportFunnelQuery, Result<List<ReportFunnelStage>>>
{
    public async Task<Result<List<ReportFunnelStage>>> Handle(
        GetAdminReportFunnelQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting admin report funnel");

        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var reportsInRange = reports.QueryAsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

        logger.LogInformation("Reports in range: {ReportsInRange}", reportsInRange);

        var submitted = await reportsInRange.CountAsync(ct).ConfigureAwait(false);
        var verified = await reportsInRange.CountAsync(r => r.VerifiedAt != null, ct).ConfigureAwait(false);
        var inProgress = await reportsInRange.CountAsync(r => r.StartedAt != null, ct).ConfigureAwait(false);
        var resolved = await reportsInRange.CountAsync(r => r.ResolvedAt != null, ct).ConfigureAwait(false);
        var closed = await reportsInRange.CountAsync(r => r.ClosedAt != null, ct).ConfigureAwait(false);

        logger.LogInformation("Submitted: {Submitted}", submitted);
        logger.LogInformation("Verified: {Verified}", verified);
        logger.LogInformation("InProgress: {InProgress}", inProgress);
        logger.LogInformation("Resolved: {Resolved}", resolved);
        logger.LogInformation("Closed: {Closed}", closed);

        logger.LogInformation("Admin report funnel retrieved successfully");

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
