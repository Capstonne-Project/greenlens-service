using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.VerifyReport;

/// <summary>
/// Officer verifies a submitted report.
/// Checks conflict of interest (BR-OFF-004, BR-ORG-012).
/// Optionally overrides severity/category and tags waste types during verification.
/// </summary>
/// <remarks>
/// Implements: BR-OFF-004 (self-report conflict), BR-ORG-012 (ward scope check),
/// BR-ORG-013 (verify step — dispatch to cleanup/inspection happens in separate commands),
/// BR-ADM-010 (audit log).
/// </remarks>
public sealed class VerifyReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IPollutionCategoryRepository pollutionCategories,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    ILocalOfficeRepository localOffices,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    IAuditLogger auditLogger,
    ILogger<VerifyReportCommandHandler> logger) : IRequestHandler<VerifyReportCommand, Result>
{
    public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
    {
        logger.LogInformation("Verifying report {ReportId} for user {UserId}", request.ReportId, currentUser.UserId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.Status != ReportStatus.Submitted)
        {
            logger.LogWarning("Report {ReportId} is not in a valid status for verification", request.ReportId);
            return Errors.Reports.InvalidStatusTransition;
        }

        // BR-OFF-004: conflict of interest — LEO cannot verify own report
        if (report.ReporterId == currentUser.UserId)
        {
            logger.LogWarning("Report {ReportId} is verified by the reporter {UserId}", request.ReportId, currentUser.UserId);
            return Errors.Reports.ConflictOfInterest;
        }

        // BR-ORG-012: LEO cannot verify reports outside their assigned ward
        // (unless report is in Department queue with no AssignedOfficeId — DEO/Admin handles)
        if (report.AssignedOfficeId.HasValue)
        {
            var leoOffice = await localOffices.QueryAsNoTracking()
                .FirstOrDefaultAsync(o => o.OfficerId == currentUser.UserId, ct)
                .ConfigureAwait(false);

            if (leoOffice is null || leoOffice.Id != report.AssignedOfficeId)
            {
                logger.LogWarning("Report {ReportId} is outside the jurisdiction of the LEO {UserId}", request.ReportId, currentUser.UserId);
                return Errors.Reports.OutsideJurisdiction;
            }
        }

        // Validate overrideCategoryId if provided
        if (request.OverrideCategoryId.HasValue)
        {
            var categoryExists = await pollutionCategories
                .ExistsActiveAsync(request.OverrideCategoryId.Value, ct)
                .ConfigureAwait(false);

            if (!categoryExists)
            {
                logger.LogWarning("Category not found for ID {CategoryId}", request.OverrideCategoryId.Value);
                return Errors.Reports.CategoryNotFound;
            }
        }

        // Validate and persist waste tags if provided
        if (request.WasteTagIds is { Count: > 0 })
        {
            var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds, ct).ConfigureAwait(false);
            if (tags.Count != request.WasteTagIds.Count)
            {
                logger.LogWarning("Waste tag not found for IDs {WasteTagIds}", request.WasteTagIds);
                return Errors.Reports.WasteTagNotFound;
            }

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
            {
                logger.LogWarning("Waste tag inactive for IDs {WasteTagIds}", request.WasteTagIds);
                return Errors.Reports.WasteTagInactive;
            }

            // Remove existing tags (in case of re-verify scenario)
            var existing = await reportWasteTags.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
            if (existing.Count > 0)
            {
                logger.LogInformation("Removing existing tags for report {ReportId}", request.ReportId);
                reportWasteTags.RemoveRange(existing);
            }

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(request.ReportId, tagId, currentUser.UserId))
                .ToList();

            logger.LogInformation("Adding new tags for report {ReportId}", request.ReportId);
            reportWasteTags.AddRange(newTags);
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            severity = report.Severity.ToString(),
            categoryId = report.CategoryId
        });

        report.Verify(currentUser.UserId, request.OverrideSeverity, request.OverrideCategoryId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,
            ReportStatus.Verified,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "VerifyReport",
            "Report",
            report.Id.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                severity = report.Severity.ToString(),
                categoryId = report.CategoryId
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} verified by LEO {UserId}", report.Id, currentUser.UserId);

        return Result.Success();
    }
}


