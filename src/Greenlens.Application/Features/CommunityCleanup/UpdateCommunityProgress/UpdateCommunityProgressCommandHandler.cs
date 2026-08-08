using Greenlens.Application.Common;
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
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
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
