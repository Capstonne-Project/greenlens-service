using Greenlens.Application.Common;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.UpdateCommunityProgress;

/// <remarks>Draft rule BR-CMU-008: only the event Leader may update progress.</remarks>
public sealed class UpdateCommunityProgressCommandHandler(
    ICommunityCleanupEventRepository events,
    IReportRepository reports,
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
    IImageExifAnalyzer exifAnalyzer,
    IGeoDistanceService geoDistance,
    ISystemSettingsProvider systemSettings,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<UpdateCommunityProgressCommandHandler> logger)
    : IRequestHandler<UpdateCommunityProgressCommand, Result<UpdateCommunityProgressResponse>>
{
    private const int MaxImages = 5;

    public async Task<Result<UpdateCommunityProgressResponse>> Handle(UpdateCommunityProgressCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.LeaderUserId != currentUser.UserId)
            return Errors.CommunityCleanup.NotEventLeader;

        if (request.ImageUrls.Count > MaxImages)
            return Errors.Media.TooManyImages;

        foreach (var url in request.ImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
                return Errors.Media.InvalidStorageUrl;
        }

        if (request.ProgressPercent < ev.ProgressPercent)
            return Errors.CommunityCleanup.ProgressCannotDecrease;

        var report = await reports.GetByIdAsync(ev.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // BR-CMU-008: progress updates must be submitted from near the site (hard block, no override).
        var targetLat = ev.MeetingLatitude ?? report.Latitude;
        var targetLng = ev.MeetingLongitude ?? report.Longitude;

        // BR-CMU-008: mobile sends lat/lng from photo EXIF; compare against event/report site.
        var locationError = await ProgressUpdateExifGuard.ValidateAsync(
                request.Latitude,
                request.Longitude,
                targetLat,
                targetLng,
                request.ImageUrls,
                fileStorage,
                exifAnalyzer,
                geoDistance,
                systemSettings,
                ct)
            .ConfigureAwait(false);

        if (locationError is not null)
        {
            logger.LogWarning(
                "Community progress location rejected for event {EventId}: {ErrorCode} — {Message}",
                request.EventId,
                locationError.Code,
                locationError.Message);
            return locationError;
        }

        try
        {
            ev.UpdateProgress(request.ProgressPercent, request.ProgressNote);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Errors.Reports.InvalidProgressPercent;
        }
        catch (InvalidOperationException)
        {
            return Errors.CommunityCleanup.InvalidStatusTransition;
        }

        var savedUrls = new List<string>(request.ImageUrls.Count);
        foreach (var url in request.ImageUrls)
        {
            var trimmed = url.Trim();
            reportMedia.Add(ReportMedia.Create(ev.ReportId, MediaType.Progress, trimmed, "image/jpeg", 0L, currentUser.UserId));
            savedUrls.Add(trimmed);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} progress updated to {Percent}% by Leader {UserId}",
            request.EventId, request.ProgressPercent, currentUser.UserId);

        return new UpdateCommunityProgressResponse(savedUrls);
    }
}
