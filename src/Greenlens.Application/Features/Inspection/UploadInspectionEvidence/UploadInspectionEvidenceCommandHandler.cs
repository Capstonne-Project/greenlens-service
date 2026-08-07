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
/// Upload inspection checklist evidence to cloud storage and persist as InspectionEvidence.
/// </summary>
/// <remarks>
/// Implements: BR-INS-033 (checklist), BR-INS-010 (≥ 2 scene photos).
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
    private const long MaxImageBytes = 20 * 1024 * 1024;
    private const long MaxVideoBytes = 30 * 1024 * 1024;
    private const long MaxAudioBytes = 10 * 1024 * 1024;

    public async Task<Result<UploadInspectionEvidenceResponse>> Handle(
        UploadInspectionEvidenceCommand request, CancellationToken ct)
    {
        if (request.Files.Count == 0)
            return Errors.Inspections.EvidenceImagesRequired;

        if (request.Category == InspectionEvidenceCategory.ViolationStatus)
            return Errors.Inspections.ChecklistViolationStatusRequired;

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

        var uploadedUrls = new List<string>();
        var folder = $"reports/{inspection.ReportId}/inspection/{inspection.Id}/{request.Category.ToString().ToLowerInvariant()}";

        foreach (var file in request.Files)
        {
            var maxSize = request.Category switch
            {
                InspectionEvidenceCategory.Video => MaxVideoBytes,
                InspectionEvidenceCategory.Audio => MaxAudioBytes,
                _ => MaxImageBytes
            };

            if (file.Bytes.LongLength > maxSize)
            {
                return Result<UploadInspectionEvidenceResponse>.Failure(new Error(
                    "FILE_TOO_LARGE",
                    $"File '{file.FileName}' exceeds size limit for {request.Category}.",
                    ErrorType.Validation));
            }

            using var stream = new MemoryStream(file.Bytes);
            var uploaded = await fileStorage.UploadAsync(
                stream, file.FileName, file.ContentType, folder, ct).ConfigureAwait(false);

            var evidence = InspectionEvidence.CreateMedia(
                inspection.Id,
                request.Category,
                uploaded.Url,
                file.ContentType,
                file.Bytes.LongLength,
                currentUser.UserId,
                file.DurationSeconds,
                request.Description);

            evidences.Add(evidence);
            uploadedUrls.Add(uploaded.Url);
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
            "Uploaded {Count} {Category} evidence for inspection {InspectionId} (total: {Total})",
            uploadedUrls.Count, request.Category, inspection.Id, totalCount);

        return new UploadInspectionEvidenceResponse(uploadedUrls, totalCount);
    }
}
