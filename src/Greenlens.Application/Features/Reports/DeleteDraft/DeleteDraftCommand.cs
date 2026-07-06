using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DeleteDraft;

/// <summary>Delete a specific draft by ID.</summary>
/// <remarks>Implements: BR-REP-019.</remarks>
public sealed record DeleteDraftCommand(Guid DraftId) : IRequest<Result<Unit>>;
