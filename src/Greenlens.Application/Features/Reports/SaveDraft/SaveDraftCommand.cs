using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.SaveDraft;

/// <summary>
/// Create or update a report draft. DraftId null = create, non-null = update.
/// </summary>
/// <remarks>Implements: BR-REP-019 — max 3 drafts per user.</remarks>
public sealed record SaveDraftCommand(
    Guid? DraftId,
    string Payload) : IRequest<Result<SaveDraftResponse>>;

public sealed record SaveDraftResponse(Guid DraftId);
