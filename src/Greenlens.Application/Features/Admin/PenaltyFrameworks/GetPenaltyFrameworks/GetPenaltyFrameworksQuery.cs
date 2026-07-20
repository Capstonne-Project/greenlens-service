using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.GetPenaltyFrameworks;

/// <summary>
/// List penalty framework entries with pagination and filters.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed record GetPenaltyFrameworksQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    ViolationLevel? ViolationLevel = null,
    bool? IsActive = null) : IRequest<Result<GetPenaltyFrameworksResponse>>;

public sealed record GetPenaltyFrameworksResponse(
    List<PenaltyFrameworkItem> Items,
    PaginationMeta Pagination);

public sealed record PenaltyFrameworkItem(
    Guid Id,
    Guid CategoryId,
    string CategoryNameVi,
    string ViolationLevel,
    decimal MinAmount,
    decimal MaxAmount,
    string Currency,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTime CreatedAt);
