using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Notifications;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

/// <summary>
/// Persist inspection checklist evidence URLs already uploaded to R2 via presigned PUT.
/// </summary>
/// <remarks>
/// Implements: BR-INS-033 (checklist), BR-INS-010 (≥ 2 scene photos), BR-SYS-002 (object storage).
/// </remarks>
public sealed class UploadInspectionEvidenceCommandHandler(
    IInspectionReportRepository inspections,
    IInspectionEvidenceRepository evidences,
    IReportRepository reports,
    ITeamMemberRepository teamMembers,
    IFileStorageService fileStorage,
    IInspectionAssignmentActivityNotifier activityNotifier,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UploadInspectionEvidenceCommandHandler> logger)
    : IRequestHandler<UploadInspectionEvidenceCommand, Result<UploadInspectionEvidenceResponse>>
{
    public async Task<Result<UploadInspectionEvidenceResponse>> Handle(
        UploadInspectionEvidenceCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return Errors.Inspections.EvidenceImagesRequired;

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        if (inspection.FieldInvestigationSubmittedAt.HasValue)
            return Errors.Inspections.FieldReportAlreadySubmitted;

        if (inspection.Status != InspectionStatus.InProgress)
            return Errors.Inspections.InvalidStatusTransition;

        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var folderPrefix = InspectionEvidenceUploadRules.BuildFolderPrefix(
            inspection.ReportId,
            inspection.Id,
            request.Category);

        var savedUrls = new List<string>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var url = item.Url.Trim();

            if (!fileStorage.IsOwnedPublicUrl(url))
            {
                logger.LogWarning("Invalid storage URL for inspection evidence: {Url}", url);
                return Errors.Media.InvalidStorageUrl;
            }

            if (!InspectionEvidenceUploadRules.UrlMatchesFolder(url, folderPrefix))
            {
                logger.LogWarning(
                    "Inspection evidence URL outside expected folder {FolderPrefix}: {Url}",
                    folderPrefix,
                    url);
                return Errors.Media.InvalidStorageUrl;
            }

            var evidence = InspectionEvidence.CreateMedia(
                inspection.Id,
                request.Category,
                url,
                item.ContentType.Trim(),
                item.SizeBytes,
                currentUser.UserId,
                item.DurationSeconds,
                request.Description);

            evidences.Add(evidence);
            savedUrls.Add(url);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        if (inspection.AssignedTeamId is Guid teamId)
        {
            var report = await reports.GetByIdAsync(inspection.ReportId, ct).ConfigureAwait(false);
            if (report is not null)
            {
                await activityNotifier.NotifyProgressUpdatedAsync(
                    inspection.CreatedByOfficerId,
                    teamId,
                    report.Id,
                    inspection.Id,
                    report.Code,
                    InspectionActivityLabels.FormatEvidenceUpload(request.Category),
                    ct).ConfigureAwait(false);
            }
        }

        var totalCount = await evidences.QueryAsNoTracking()
            .CountAsync(
                e => e.InspectionReportId == inspection.Id && e.Category == request.Category,
                ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Saved {Count} {Category} evidence URLs for inspection {InspectionId} (total: {Total})",
            savedUrls.Count, request.Category, inspection.Id, totalCount);

        return new UploadInspectionEvidenceResponse(savedUrls, totalCount);
    }
}
