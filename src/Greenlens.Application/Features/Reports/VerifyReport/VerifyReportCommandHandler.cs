using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.VerifyReport;

/// <summary>
/// Officer verifies a submitted report. Checks conflict of interest (BR-OFF-004).
/// Optionally overrides severity/category and tags waste types during verification.
/// </summary>
public sealed class VerifyReportCommandHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IPollutionCategoryRepository pollutionCategories,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<VerifyReportCommandHandler> logger) : IRequestHandler<VerifyReportCommand, Result>
{
    public async Task<Result> Handle(VerifyReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.Submitted)
            return Errors.Reports.InvalidStatusTransition;

        // BR-OFF-004: conflict of interest
        if (report.ReporterId == currentUser.UserId)
            return Errors.Reports.ConflictOfInterest;

        // Validate overrideCategoryId if provided
        if (request.OverrideCategoryId.HasValue)
        {
            var categoryExists = await pollutionCategories
                .ExistsActiveAsync(request.OverrideCategoryId.Value, ct)
                .ConfigureAwait(false);

            if (!categoryExists)
                return Errors.Reports.CategoryNotFound;
        }

        // Validate and persist waste tags if provided
        if (request.WasteTagIds is { Count: > 0 })
        {
            var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds, ct).ConfigureAwait(false);
            if (tags.Count != request.WasteTagIds.Count)
                return Errors.Reports.WasteTagNotFound;

            var inactiveTags = tags.Where(t => !t.IsActive).ToList();
            if (inactiveTags.Count > 0)
                return Errors.Reports.WasteTagInactive;

            // Remove existing tags (in case of re-verify scenario)
            var existing = await reportWasteTags.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
            if (existing.Count > 0)
                reportWasteTags.RemoveRange(existing);

            var newTags = request.WasteTagIds
                .Select(tagId => ReportWasteTag.Create(request.ReportId, tagId, currentUser.UserId))
                .ToList();

            reportWasteTags.AddRange(newTags);
        }

        report.Verify(currentUser.UserId, request.OverrideSeverity, request.OverrideCategoryId);

        var history = ReportStatusHistory.Create(
            report.Id,
            ReportStatus.Submitted,
            ReportStatus.Verified,
            currentUser.UserId);

        statusHistory.Add(history);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} verified by LEO {UserId}", report.Id, currentUser.UserId);

        return Result.Success();
    }
}

