using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.UploadInspectionEvidence;

/// <summary>
/// Upload inspection evidence photos to cloud storage and persist as ReportMedia (MediaType.Inspection).
/// </summary>
/// <remarks>
/// Implements: BR-INS-010 — biên bản hiện trường cần ≥ 2 ảnh.
/// Images belong to the parent Report's media collection but typed as Inspection.
/// </remarks>
public sealed class UploadInspectionEvidenceCommandHandler(
    IInspectionReportRepository inspections,
    IReportMediaRepository reportMedia,
    ITeamMemberRepository teamMembers,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UploadInspectionEvidenceCommandHandler> logger)
    : IRequestHandler<UploadInspectionEvidenceCommand, Result<UploadInspectionEvidenceResponse>>
{
    public async Task<Result<UploadInspectionEvidenceResponse>> Handle(
        UploadInspectionEvidenceCommand request, CancellationToken ct)
    {
        if (request.Images.Count == 0)
            return Errors.Inspections.EvidenceImagesRequired;

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct)
            .ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        // Only allow upload while Draft or InProgress
        if (inspection.Status is not (InspectionStatus.Draft or InspectionStatus.InProgress))
            return Errors.Inspections.InvalidStatusTransition;

        // Verify caller is part of the assigned inspection team
        var authError = await InspectionTeamAuthorization.ValidateTeamMemberAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        // Upload images to cloud storage and persist as ReportMedia
        var uploadedUrls = new List<string>();
        var folder = $"reports/{inspection.ReportId}/inspection/{inspection.Id}";

        foreach (var image in request.Images)
        {
            using var stream = new MemoryStream(image.Bytes);
            var uploaded = await fileStorage.UploadAsync(
                stream, image.FileName, image.ContentType, folder, ct)
                .ConfigureAwait(false);

            var media = ReportMedia.Create(
                inspection.ReportId,
                MediaType.Inspection,
                uploaded.Url,
                image.ContentType,
                image.Bytes.LongLength,
                currentUser.UserId);
            reportMedia.Add(media);

            uploadedUrls.Add(uploaded.Url);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        // Count total evidence for this inspection (existing + newly uploaded)
        var totalCount = await reportMedia.QueryAsNoTracking()
            .CountAsync(
                m => m.ReportId == inspection.ReportId && m.Type == MediaType.Inspection,
                ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded {Count} inspection evidence photos for InspectionReport {InspectionId} (total: {Total})",
            uploadedUrls.Count, inspection.Id, totalCount);

        return new UploadInspectionEvidenceResponse(uploadedUrls, totalCount);
    }
}
