using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.UploadBeforeImages;

/// <summary>
/// Upload before images (current state of pollution site) after team check-in.
/// </summary>
/// <remarks>
/// Implements: BR-REP-014 — before images are required before Resolve.
/// Team leader uploads these after arriving at the site and before starting work.
/// Images are stored as ReportMedia with MediaType.Before.
/// </remarks>
public sealed class UploadBeforeImagesCommandHandler(
    IReportRepository reports,
    IReportAssignmentRepository assignments,
    IReportMediaRepository reportMedia,
    ITeamMemberRepository teamMembers,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UploadBeforeImagesCommandHandler> logger)
    : IRequestHandler<UploadBeforeImagesCommand, Result<UploadBeforeImagesResponse>>
{
    public async Task<Result<UploadBeforeImagesResponse>> Handle(
        UploadBeforeImagesCommand request,
        CancellationToken ct)
    {
        if (request.Images.Count == 0)
            return Errors.Reports.MissingBeforeImages;

        // Resolve team leader from JWT
        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (leader is null)
            return Errors.Reports.NotTeamLeader;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        // Verify team has an active assignment for this report
        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct)
            .ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == leader.TeamId);
        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.AssignmentNotInProgress;

        // Upload images to cloud storage and persist as ReportMedia
        var uploadedUrls = new List<string>();
        var folder = $"reports/{request.ReportId}/before/{leader.TeamId}";

        foreach (var image in request.Images)
        {
            using var stream = new MemoryStream(image.Bytes);
            var uploaded = await fileStorage.UploadAsync(
                stream, image.FileName, image.ContentType, folder, ct)
                .ConfigureAwait(false);

            var media = ReportMedia.Create(
                request.ReportId,
                MediaType.Before,
                uploaded.Url,
                image.ContentType,
                image.Bytes.LongLength,
                currentUser.UserId);
            reportMedia.Add(media);

            uploadedUrls.Add(uploaded.Url);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Uploaded {Count} before images for report {ReportId} by team {TeamId}",
            uploadedUrls.Count, request.ReportId, leader.TeamId);

        return new UploadBeforeImagesResponse(uploadedUrls);
    }
}
