using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.SaveDraft;

/// <summary>
/// Create or update a report draft. Max 3 per user.
/// </summary>
/// <remarks>Implements: BR-REP-019.</remarks>
public sealed class SaveDraftCommandHandler(
    IReportDraftRepository drafts,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<SaveDraftCommandHandler> logger)
    : IRequestHandler<SaveDraftCommand, Result<SaveDraftResponse>>
{
    private const int MaxDraftsPerUser = 3;
    public async Task<Result<SaveDraftResponse>> Handle(
        SaveDraftCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Saving draft for user {UserId}", currentUser.UserId);

        // Update existing draft
        if (request.DraftId.HasValue)
        {
            logger.LogInformation("Updating existing draft for user {UserId}", currentUser.UserId);
            var existing = await drafts.GetByIdAsync(request.DraftId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null || existing.UserId != currentUser.UserId)
            {
                logger.LogWarning("Draft not found for ID {DraftId}", request.DraftId.Value);
                return Errors.Reports.DraftNotFound;
            }

            existing.UpdatePayload(request.Payload);
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new SaveDraftResponse(existing.Id);
        }

        // Create new — check limit
        var count = await drafts.Query()
            .CountAsync(d => d.UserId == currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (count >= MaxDraftsPerUser)
        {
            logger.LogWarning("Draft limit reached for user {UserId}", currentUser.UserId);
            return Errors.Reports.DraftLimitReached;
        }

        var draft = ReportDraft.Create(currentUser.UserId, request.Payload);
        drafts.Add(draft);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Draft saved for user {UserId}", currentUser.UserId);

        return new SaveDraftResponse(draft.Id);
    }
}
