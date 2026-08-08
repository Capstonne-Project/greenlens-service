using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common;

/// <summary>Shared BR-REP-015 eligibility checks for citizen reopen requests.</summary>
public static class ReopenRequestEligibility
{
    public static Error? ValidateCitizenCanRequest(Report report, DateTime utcNow)
    {
        if (report.Status == ReportStatus.Closed)
            return Errors.Reports.CannotReopenFromClosed;

        if (report.Status != ReportStatus.Resolved)
            return Errors.Reports.CannotReopenNotResolved;

        if (report.ResolvedAt.HasValue && utcNow - report.ResolvedAt.Value > TimeSpan.FromDays(7))
            return Errors.Reports.ReopenWindowExpired;

        if (report.HasPendingReopenRequest)
            return Errors.Reports.PendingReopenRequestExists;

        if (report.ReopenedCount >= Report.MaxApprovedReopens)
            return Errors.Reports.ReopenLimitReached;

        return null;
    }
}
