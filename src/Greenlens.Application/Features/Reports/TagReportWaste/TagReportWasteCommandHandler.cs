using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.TagReportWaste;

/// <summary>
/// Replaces all waste tags on a report. Validates tag IDs exist and are active.
/// Only DEO/Admin can tag. Report must be Submitted, Verified, Dispatched, or InProgress.
/// </summary>
public sealed class TagReportWasteCommandHandler(
    IReportRepository reports,
    IWasteTagRepository wasteTags,
    IReportWasteTagRepository reportWasteTags,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<TagReportWasteCommandHandler> logger) : IRequestHandler<TagReportWasteCommand, Result>
{
    private static readonly HashSet<ReportStatus> AllowedStatuses =
    [
        ReportStatus.Submitted,
        ReportStatus.Verified,
        ReportStatus.Dispatched,
        ReportStatus.InProgress
    ];

    public async Task<Result> Handle(TagReportWasteCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (!AllowedStatuses.Contains(report.Status))
            return Errors.Reports.InvalidStatusTransition;

        // Validate all tag IDs exist and are active
        var tags = await wasteTags.GetByIdsAsync(request.WasteTagIds, ct).ConfigureAwait(false);
        if (tags.Count != request.WasteTagIds.Count)
            return Errors.Reports.WasteTagNotFound;

        var inactiveTags = tags.Where(t => !t.IsActive).ToList();
        if (inactiveTags.Count > 0)
            return Errors.Reports.WasteTagInactive;

        // Remove existing tags
        var existingTags = await reportWasteTags.GetByReportIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (existingTags.Count > 0)
            reportWasteTags.RemoveRange(existingTags);

        // Add new tags
        var newTags = request.WasteTagIds
            .Select(tagId => ReportWasteTag.Create(request.ReportId, tagId, currentUser.UserId))
            .ToList();

        reportWasteTags.AddRange(newTags);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} tagged with {TagCount} waste tags by {UserId}",
            request.ReportId, request.WasteTagIds.Count, currentUser.UserId);

        return Result.Success();
    }
}
