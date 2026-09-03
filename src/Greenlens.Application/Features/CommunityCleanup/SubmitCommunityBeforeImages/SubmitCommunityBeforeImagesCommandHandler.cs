using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.SubmitCommunityBeforeImages;

/// <remarks>
/// Implements: BR-CMU-008 (Leader uploads before images), BR-REP-014 (before images on report).
/// Raises <see cref="CommunityCleanupBeforeImagesUploadedEvent"/> so the LEO is notified via outbox.
/// </remarks>
public sealed class SubmitCommunityBeforeImagesCommandHandler(
    ICommunityCleanupEventRepository events,
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
    ISystemSettingsProvider systemSettings,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<SubmitCommunityBeforeImagesCommandHandler> logger)
    : IRequestHandler<SubmitCommunityBeforeImagesCommand, Result<SubmitCommunityBeforeImagesResponse>>
{
    public async Task<Result<SubmitCommunityBeforeImagesResponse>> Handle(SubmitCommunityBeforeImagesCommand request, CancellationToken ct)
    {
        var maxImages = ModuleSystemSettings.CommunityBeforeImagesMax(systemSettings);

        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.LeaderUserId != currentUser.UserId)
            return Errors.CommunityCleanup.NotEventLeader;

        if (request.ImageUrls.Count == 0)
            return Errors.Reports.MissingBeforeImages;

        if (request.ImageUrls.Count > maxImages)
            return Errors.Media.TooManyImages;

        foreach (var url in request.ImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
                return Errors.Media.InvalidStorageUrl;
        }

        var savedUrls = new List<string>(request.ImageUrls.Count);
        foreach (var url in request.ImageUrls)
        {
            var trimmed = url.Trim();
            reportMedia.Add(ReportMedia.Create(ev.ReportId, MediaType.Before, trimmed, "image/jpeg", 0L, currentUser.UserId));
            savedUrls.Add(trimmed);
        }

        // Domain event → LEO notification sau commit (UnitOfWork dispatches domain events).
        ev.AddDomainEvent(new CommunityCleanupBeforeImagesUploadedEvent(
            ev.Id, ev.Title, ev.CreatedByLeoId, savedUrls.Count));

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId}: {Count} before images saved by Leader {UserId}",
            request.EventId, savedUrls.Count, currentUser.UserId);

        return new SubmitCommunityBeforeImagesResponse(savedUrls);
    }
}
