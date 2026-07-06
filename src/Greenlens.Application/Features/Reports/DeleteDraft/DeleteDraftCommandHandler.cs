using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;

namespace Greenlens.Application.Features.Reports.DeleteDraft;

/// <summary>BR-REP-019: Hard-delete draft after ownership check.</summary>
public sealed class DeleteDraftCommandHandler(
    IGenericRepository<ReportDraft> drafts,
    ICurrentUser currentUser,
    IUnitOfWork uow)
    : IRequestHandler<DeleteDraftCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        DeleteDraftCommand request,
        CancellationToken cancellationToken)
    {
        var draft = await drafts.GetByIdAsync(request.DraftId, cancellationToken)
            .ConfigureAwait(false);

        if (draft is null || draft.UserId != currentUser.UserId)
            return Errors.Reports.DraftNotFound;

        drafts.Remove(draft);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
