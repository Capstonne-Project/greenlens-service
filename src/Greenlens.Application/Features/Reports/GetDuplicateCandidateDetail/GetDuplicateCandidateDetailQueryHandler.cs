using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetDuplicateCandidateDetail;

/// <summary>
/// Returns side-by-side detail for a report flagged as a possible duplicate and its primary report.
/// </summary>
/// <remarks>
/// Implements: BR-REP-031 (possible duplicate flag), BR-REP-032 (merge review).
/// Scope: LEO → assigned office; DEO → department; Admin → all.
/// </remarks>
public sealed class GetDuplicateCandidateDetailQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetDuplicateCandidateDetailQueryHandler> logger)
    : IRequestHandler<GetDuplicateCandidateDetailQuery, Result<DuplicateCandidateDetailResponse>>
{
    public async Task<Result<DuplicateCandidateDetailResponse>> Handle(
        GetDuplicateCandidateDetailQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting duplicate candidate detail for report {ReportId}", request.ReportId);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
        {
            logger.LogWarning("User not found for duplicate candidate detail: {UserId}", currentUser.UserId);
            return Errors.Users.UserNotFound;
        }

        var report = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!report.IsPossibleDuplicate || !report.PossibleDuplicateOfReportId.HasValue)
        {
            logger.LogWarning("Report {ReportId} has no possible-duplicate flag", request.ReportId);
            return Errors.Reports.NotPossibleDuplicate;
        }

        var primaryId = report.PossibleDuplicateOfReportId.Value;

        var primary = await reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == primaryId, ct)
            .ConfigureAwait(false);

        if (primary is null)
        {
            logger.LogWarning("Primary report {PrimaryId} not found", primaryId);
            return Errors.Reports.PrimaryReportNotFound;
        }

        var accessError = ReportReviewCandidateFilters.ValidateDuplicateCandidateAccess(
            report, primary, user, currentUser.Role);
        if (accessError is not null)
        {
            logger.LogWarning(
                "User {UserId} denied duplicate detail for report {ReportId}: {ErrorCode}",
                currentUser.UserId, request.ReportId, accessError.Code);
            return accessError;
        }

        var distance = GeoMath.HaversineMeters(
            report.Latitude, report.Longitude,
            primary.Latitude, primary.Longitude);

        var hoursSincePrimaryCreated = (report.CreatedAt - primary.CreatedAt).TotalHours;

        return new DuplicateCandidateDetailResponse(
            MapSide(report),
            MapSide(primary),
            report.DuplicateDetectionSource,
            report.AiSimilarityScore,
            distance,
            hoursSincePrimaryCreated);
    }

    private static DuplicateCandidateReportSide MapSide(Report report)
    {
        var media = report.Media
            .OrderBy(m => m.UploadedAt)
            .Select(m => new DuplicateCandidateMediaItem(
                m.Id, m.Url, m.ThumbnailUrl, m.Type, m.UploadedAt))
            .ToList();

        return new DuplicateCandidateReportSide(
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
            media);
    }
}
