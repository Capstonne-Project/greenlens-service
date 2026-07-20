using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.BlockedWords.GetBlockedWords;

/// <summary>Lists blocked words for admin moderation dashboard.</summary>
/// <remarks>Implements: BR-REP-004, BR-CMT-003, BR-ADM-010.</remarks>
public sealed record GetBlockedWordsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null) : IRequest<Result<GetBlockedWordsResponse>>;

public sealed record GetBlockedWordsResponse(
    IReadOnlyList<BlockedWordItem> Items,
    int TotalCount);

public sealed record BlockedWordItem(
    Guid Id,
    string Word,
    string? Note,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
