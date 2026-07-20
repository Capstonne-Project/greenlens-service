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
/// Persist before images already uploaded to R2 via presigned URL.
/// </summary>
/// <remarks>
/// Implements: BR-REP-014 — before images required before Resolve.
/// Client uploads files directly to R2, then posts public URLs here.
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
    private const int MaxImages = 5;

    public async Task<Result<UploadBeforeImagesResponse>> Handle(
        UploadBeforeImagesCommand request,
        CancellationToken ct)
    {
        if (request.ImageUrls.Count == 0)
            return Errors.Reports.MissingBeforeImages;

        if (request.ImageUrls.Count > MaxImages)
            return Errors.Media.TooManyImages;

        foreach (var url in request.ImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
                return Errors.Media.InvalidStorageUrl;
        }

        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (leader is null)
            return Errors.Reports.NotTeamLeader;

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        if (report.Status != ReportStatus.InProgress)
            return Errors.Reports.InvalidStatusTransition;

        var reportAssignments = await assignments.GetByReportIdAsync(request.ReportId, ct)
            .ConfigureAwait(false);
        var assignment = reportAssignments.FirstOrDefault(a => a.TeamId == leader.TeamId);
        if (assignment is null)
            return Errors.Reports.AssignmentNotFound;

        if (assignment.Status != AssignmentStatus.InProgress)
            return Errors.Reports.AssignmentNotInProgress;

        var savedUrls = new List<string>(request.ImageUrls.Count);
        foreach (var url in request.ImageUrls)
        {
            var trimmed = url.Trim();
            var media = ReportMedia.Create(
                request.ReportId,
                MediaType.Before,
                trimmed,
                "image/jpeg",
                0L,
                currentUser.UserId);
            reportMedia.Add(media);
            savedUrls.Add(trimmed);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Saved {Count} before image URLs for report {ReportId} by team {TeamId}",
            savedUrls.Count, request.ReportId, leader.TeamId);

        return new UploadBeforeImagesResponse(savedUrls);
    }
}
