using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetViolationRecurrenceComparison;

/// <summary>
/// Returns side-by-side data for the current report and the prior Closed report (BR-REP-034).
/// </summary>
/// <remarks>
/// Implements: BR-REP-034, BR-OFF-005 (LEO triage support).
/// Scope: LEO → assigned office; DEO → department; Admin → all.
/// </remarks>
public sealed class GetViolationRecurrenceComparisonQueryHandler(
    IReportRepository reports,
    IInspectionReportRepository inspections,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetViolationRecurrenceComparisonQueryHandler> logger)
    : IRequestHandler<GetViolationRecurrenceComparisonQuery, Result<ViolationRecurrenceComparisonResponse>>
{
    public async Task<Result<ViolationRecurrenceComparisonResponse>> Handle(
        GetViolationRecurrenceComparisonQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting violation recurrence comparison for report {ReportId}", request.ReportId);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for violation recurrence comparison: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var current = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (current is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!current.IsSuspectedViolationRecurrence || !current.SuspectedRecurrenceOfReportId.HasValue)
        {
            logger.LogWarning("Report {ReportId} has no violation recurrence flag", request.ReportId);
            return Errors.Reports.NotSuspectedViolationRecurrence;
        }

        var accessError = ReportReviewCandidateFilters.ValidateReportAccess(
            current, user, currentUser.Role);
        if (accessError is not null)
        {
            logger.LogWarning(
                "User {UserId} denied recurrence comparison for report {ReportId}: {ErrorCode}",
                currentUser.UserId, request.ReportId, accessError.Code);
            return accessError;
        }

        var priorId = current.SuspectedRecurrenceOfReportId.Value;

        var prior = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == priorId, ct)
            .ConfigureAwait(false);

        if (prior is null)
        {
            logger.LogWarning("Prior closed report {PriorId} not found", priorId);
            return Errors.Reports.ReportNotFound;
        }

        var currentInspections = await inspections.GetByReportIdAsync(current.Id, ct).ConfigureAwait(false);
        var currentInspection = currentInspections
            .OrderByDescending(ir => ir.CreatedAt)
            .FirstOrDefault();

        var priorInspections = await inspections.GetByReportIdAsync(priorId, ct).ConfigureAwait(false);
        var priorInspection = priorInspections
            .OrderByDescending(ir => ir.CreatedAt)
            .FirstOrDefault();

        var distance = GeoMath.HaversineMeters(
            current.Latitude, current.Longitude,
            prior.Latitude, prior.Longitude);

        var daysSinceClosed = prior.ClosedAt.HasValue
            ? (int)Math.Floor((DateTime.UtcNow - prior.ClosedAt.Value).TotalDays)
            : 0;

        return new ViolationRecurrenceComparisonResponse(
            MapSide(current, hasInspection: currentInspection is not null),
            MapSide(prior, priorInspection?.Id, priorInspection?.Status.ToString(), priorInspection is not null),
            daysSinceClosed,
            distance);
    }

    private static ViolationRecurrenceReportSide MapSide(
        Domain.Entities.Report report,
        Guid? inspectionId = null,
        string? inspectionStatus = null,
        bool hasInspection = false)
    {
        var media = report.Media
            .OrderBy(m => m.UploadedAt)
            .Select(m => new ViolationRecurrenceMediaItem(
                m.Id, m.Url, m.ThumbnailUrl, m.Type, m.UploadedAt))
            .ToList();

        return new ViolationRecurrenceReportSide(
            report.Id,
            report.Code,
            report.Status,
            report.Category.Code,
            report.Category.NameVi,
            report.Severity,
            report.Description,
            report.Latitude,
            report.Longitude,
            report.Address,
            report.CreatedAt,
            report.ClosedAt,
            media,
            inspectionId.HasValue,
            inspectionId,
            inspectionStatus,
            hasInspection);
    }
}
