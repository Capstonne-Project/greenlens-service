using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.CommunityCleanup.SubmitCommunityVerification;

/// <remarks>
/// Draft rule BR-CMU-009: ≥ 1 before + ≥ 2 after images, progress at 100%, before LEO review.
/// </remarks>
public sealed class SubmitCommunityVerificationCommandHandler(
    ICommunityCleanupEventRepository events,
    IReportMediaRepository reportMedia,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<SubmitCommunityVerificationCommandHandler> logger) : IRequestHandler<SubmitCommunityVerificationCommand, Result>
{
    public async Task<Result> Handle(SubmitCommunityVerificationCommand request, CancellationToken ct)
    {
        var ev = await events.GetByIdAsync(request.EventId, ct).ConfigureAwait(false);
        if (ev is null)
            return Errors.CommunityCleanup.EventNotFound;

        if (ev.LeaderUserId != currentUser.UserId)
            return Errors.CommunityCleanup.NotEventLeader;

        if (ev.Status != CommunityCleanupStatus.InProgress)
            return Errors.CommunityCleanup.InvalidStatusTransition;

        if (ev.ProgressPercent < 100)
            return Errors.CommunityCleanup.ProgressNotComplete;

        if (request.AfterImageUrls.Count < 2)
            return Errors.CommunityCleanup.InsufficientVerificationEvidence;

        foreach (var url in request.AfterImageUrls)
        {
            if (!fileStorage.IsOwnedPublicUrl(url))
                return Errors.Media.InvalidStorageUrl;
        }

        var hasBeforeImage = await reportMedia.QueryAsNoTracking()
            .AnyAsync(m => m.ReportId == ev.ReportId && m.Type == MediaType.Before, ct)
            .ConfigureAwait(false);
        if (!hasBeforeImage)
            return Errors.CommunityCleanup.InsufficientVerificationEvidence;

        foreach (var url in request.AfterImageUrls)
            reportMedia.Add(ReportMedia.Create(ev.ReportId, MediaType.After, url.Trim(), "image/jpeg", 0L, currentUser.UserId));

        ev.SubmitVerification();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Community cleanup {EventId} submitted for verification by Leader {UserId}", request.EventId, currentUser.UserId);
        return Result.Success();
    }
}
