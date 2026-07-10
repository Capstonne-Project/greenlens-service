using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.SaveDraft;

/// <summary>
/// Create or update a report draft. Max 3 per user.
/// </summary>
/// <remarks>Implements: BR-REP-019.</remarks>
public sealed class SaveDraftCommandHandler(
    IReportDraftRepository drafts,
    ICurrentUser currentUser,
    IUnitOfWork uow)
    : IRequestHandler<SaveDraftCommand, Result<SaveDraftResponse>>
{
    private const int MaxDrafts = 3;

    public async Task<Result<SaveDraftResponse>> Handle(
        SaveDraftCommand request,
        CancellationToken cancellationToken)
    {
        // Update existing draft
        if (request.DraftId.HasValue)
        {
            var existing = await drafts.GetByIdAsync(request.DraftId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null || existing.UserId != currentUser.UserId)
                return Errors.Reports.DraftNotFound;

            existing.UpdatePayload(request.Payload);
            await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new SaveDraftResponse(existing.Id);
        }

        // Create new — check limit
        var count = await drafts.Query()
            .CountAsync(d => d.UserId == currentUser.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (count >= MaxDrafts)
            return Errors.Reports.DraftLimitReached;

        var draft = ReportDraft.Create(currentUser.UserId, request.Payload);
        drafts.Add(draft);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SaveDraftResponse(draft.Id);
    }
}
