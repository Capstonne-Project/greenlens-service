using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.FlagReport;

/// <summary>
/// A citizen flags a report. When a report accumulates 3+ distinct flags of the same type,
/// the responsible LEO(s) are notified to review it.
/// </summary>
/// <remarks>
/// Implements: BR-REP-033 (≥3 different flags → notify LEO). One flag per (report, user, type)
/// is enforced by a unique index on report_flags.
/// </remarks>
public sealed class FlagReportCommandHandler(
    IReportRepository reports,
    IApplicationDbContext db,
    ICurrentUser currentUser,
    INotificationService notifications,
    IUnitOfWork uow,
    ILogger<FlagReportCommandHandler> logger) : IRequestHandler<FlagReportCommand, Result>
{
    private const int NotifyThreshold = 3;

    public async Task<Result> Handle(FlagReportCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
            return Errors.Reports.LoginRequired;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // BR-REP-033: cannot flag your own report.
        if (report.ReporterId == currentUser.UserId)
            return Errors.Reports.CannotFlagOwnReport;

        var alreadyFlagged = await db.Set<ReportFlag>()
            .AnyAsync(
                f => f.ReportId == request.ReportId
                     && f.FlaggerId == currentUser.UserId
                     && f.FlagType == request.Type,
                ct)
            .ConfigureAwait(false);
        if (alreadyFlagged)
            return Errors.Reports.AlreadyFlagged;

        db.Set<ReportFlag>().Add(
            ReportFlag.Create(request.ReportId, currentUser.UserId, request.Type, request.Reason));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        var flagCount = await db.Set<ReportFlag>()
            .CountAsync(f => f.ReportId == request.ReportId && f.FlagType == request.Type, ct)
            .ConfigureAwait(false);

        if (flagCount >= NotifyThreshold)
            await NotifyReviewersAsync(report, request.Type, flagCount, ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} flagged as {FlagType} by {UserId} (total {Count})",
            report.Id, request.Type, currentUser.UserId, flagCount);

        return Result.Success();
    }

    private async Task NotifyReviewersAsync(Report report, FlagType type, int count, CancellationToken ct)
    {
        if (report.AssignedOfficeId is null)
            return;

        var reviewerIds = await db.Set<User>()
            .Where(u => u.Role == UserRole.LEO
                        && u.LocalOfficeId == report.AssignedOfficeId
                        && !u.IsBanned)
            .Select(u => u.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var reviewerId in reviewerIds)
        {
            await notifications.SendFromTemplateAsync(
                reviewerId,
                NotificationType.DuplicateReviewNeeded,
                new Dictionary<string, string>
                {
                    ["report_code"] = report.Code,
                    ["flag_count"] = count.ToString(),
                    ["flag_type"] = type.ToString()
                },
                report.Id,
                ct).ConfigureAwait(false);
        }
    }
}
