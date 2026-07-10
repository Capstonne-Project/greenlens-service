using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyDrafts;

/// <summary>Get all drafts for the current user.</summary>
/// <remarks>Implements: BR-REP-019.</remarks>
public sealed record GetMyDraftsQuery() : IRequest<Result<GetMyDraftsResponse>>;

public sealed record GetMyDraftsResponse(IReadOnlyList<DraftItemDto> Drafts);

public sealed record DraftItemDto(
    Guid Id,
    string Payload,
    DateTime CreatedAt,
    DateTime UpdatedAt);
