using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetOfficerKpi;

/// <summary>
/// BR-OFF-021: Compute KPI metrics for an officer within a time period.
/// Metrics: verified on-time %, resolved rate, avg response time, totals.
/// </summary>
public sealed class GetOfficerKpiQueryHandler(
    IReportStatusHistoryRepository statusHistoryRepo,
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetOfficerKpiQueryHandler> logger)
    : IRequestHandler<GetOfficerKpiQuery, Result<OfficerKpiResponse>>
{
    public async Task<Result<OfficerKpiResponse>> Handle(
        GetOfficerKpiQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting officer KPI for user {UserId}", currentUser.UserId);

        // Resolve officer ID — LEO sees own, DEO/Admin can specify
        var officerId = request.OfficerId ?? currentUser.UserId;

        var officer = await users.GetByIdAsync(officerId, cancellationToken)
            .ConfigureAwait(false);
        if (officer is null)
        {
            logger.LogWarning("Officer not found for ID {UserId}", officerId);
            return Errors.Users.UserNotFound;
        }

        // Resolve period
        var (from, to) = ResolvePeriod(request);

        // ── Query status history for this officer in period ──
        var histories = statusHistoryRepo.QueryAsNoTracking()
            .Where(h => h.ChangedBy == officerId
                     && h.CreatedAt >= from
                     && h.CreatedAt <= to);

        // Verified: Submitted → Verified
        var verifiedHistory = await histories
            .Where(h => h.FromStatus == ReportStatus.Submitted
                     && h.ToStatus == ReportStatus.Verified)
            .Select(h => new { h.ReportId, h.CreatedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var totalVerified = verifiedHistory.Count;

        // Get SLA verification data for those reports
        var verifiedReportIds = verifiedHistory.Select(v => v.ReportId).ToList();
        var verifiedReports = totalVerified > 0
            ? await reports.QueryAsNoTracking()
                .Where(r => verifiedReportIds.Contains(r.Id))
                .Select(r => new { r.Id, r.SlaVerifyBreached })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

        var verifiedOnTime = verifiedReports.Count(r => !r.SlaVerifyBreached);

        // Rejected: Submitted → Submitted (reject re-queues)
        var totalRejected = await histories
            .CountAsync(h => h.FromStatus == ReportStatus.Submitted
                          && h.ToStatus == ReportStatus.Submitted
                          && h.Reason != null, cancellationToken)
            .ConfigureAwait(false);

        // Escalated count — from Verified/InProgress with reason containing "escalat"
        var totalEscalated = await histories
            .CountAsync(h => h.Metadata != null
                          && h.Metadata.Contains("escalat"), cancellationToken)
            .ConfigureAwait(false);

        // Resolution: InProgress → Resolved
        var totalResolved = await histories
            .CountAsync(h => h.ToStatus == ReportStatus.Resolved, cancellationToken)
            .ConfigureAwait(false);

        // Closed
        var totalClosed = await histories
            .CountAsync(h => h.ToStatus == ReportStatus.Closed, cancellationToken)
            .ConfigureAwait(false);

        // Resolved rate: (resolved + closed) / total verified
        var resolvedRate = totalVerified > 0
            ? Math.Round((decimal)(totalResolved + totalClosed) / totalVerified * 100, 1)
            : 0m;

        // Avg response time: average of (verifiedAt - report.CreatedAt) for verified reports
        decimal avgResponseTimeHours = 0;
        if (totalVerified > 0)
        {
            var reportCreatedDates = await reports.QueryAsNoTracking()
                .Where(r => verifiedReportIds.Contains(r.Id))
                .Select(r => new { r.Id, r.CreatedAt })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var totalHours = 0m;
            foreach (var vh in verifiedHistory)
            {
                var reportCreated = reportCreatedDates
                    .FirstOrDefault(r => r.Id == vh.ReportId);

                if (reportCreated is not null)
                    totalHours += (decimal)(vh.CreatedAt - reportCreated.CreatedAt).TotalHours;
            }

            avgResponseTimeHours = Math.Round(totalHours / totalVerified, 1);
        }

        var officerName = officer.FullName;

        return new OfficerKpiResponse(
            officerId,
            officerName,
            from,
            to,
            totalVerified,
            verifiedOnTime,
            totalVerified > 0
                ? Math.Round((decimal)verifiedOnTime / totalVerified * 100, 1)
                : 0m,
            totalRejected,
            totalEscalated,
            totalResolved,
            totalClosed,
            resolvedRate,
            avgResponseTimeHours);
    }

    private static (DateTime from, DateTime to) ResolvePeriod(GetOfficerKpiQuery request)
    {
        if (request.From.HasValue && request.To.HasValue)
            return (DateTime.SpecifyKind(request.From.Value, DateTimeKind.Utc),
                    DateTime.SpecifyKind(request.To.Value, DateTimeKind.Utc));

        var now = DateTime.UtcNow;

        return request.Period switch
        {
            KpiPeriod.ThisMonth => (Utc(now.Year, now.Month, 1), now),
            KpiPeriod.LastMonth => (Utc(now.Year, now.Month, 1).AddMonths(-1),
                                   Utc(now.Year, now.Month, 1).AddSeconds(-1)),
            KpiPeriod.ThisQuarter => (GetQuarterStart(now), now),
            KpiPeriod.LastQuarter => (GetQuarterStart(now).AddMonths(-3),
                                     GetQuarterStart(now).AddSeconds(-1)),
            KpiPeriod.ThisYear => (Utc(now.Year, 1, 1), now),
            KpiPeriod.LastYear => (Utc(now.Year - 1, 1, 1),
                                  Utc(now.Year, 1, 1).AddSeconds(-1)),
            // Default: this month
            _ => (Utc(now.Year, now.Month, 1), now)
        };
    }

    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime GetQuarterStart(DateTime date)
    {
        var quarterMonth = ((date.Month - 1) / 3) * 3 + 1;
        return new DateTime(date.Year, quarterMonth, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
