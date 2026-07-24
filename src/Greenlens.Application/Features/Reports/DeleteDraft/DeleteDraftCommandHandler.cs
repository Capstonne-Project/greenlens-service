using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DeleteDraft;

/// <summary>BR-REP-019: Hard-delete draft after ownership check.</summary>
public sealed class DeleteDraftCommandHandler(
    IReportDraftRepository drafts,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeleteDraftCommandHandler> logger)
    : IRequestHandler<DeleteDraftCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        DeleteDraftCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting draft for report {DraftId}", request.DraftId);

        var draft = await drafts.GetByIdAsync(request.DraftId, cancellationToken)
            .ConfigureAwait(false);

        if (draft is null || draft.UserId != currentUser.UserId)
        {
            logger.LogWarning("Draft not found for ID {DraftId}", request.DraftId);
            return Errors.Reports.DraftNotFound;
        }

        drafts.Remove(draft);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
