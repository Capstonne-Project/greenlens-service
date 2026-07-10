using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.GetPenaltyFrameworks;

/// <summary>
/// Returns a paginated list of penalty framework entries with optional filters.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class GetPenaltyFrameworksQueryHandler(DbContext db)
    : IRequestHandler<GetPenaltyFrameworksQuery, Result<GetPenaltyFrameworksResponse>>
{
    public async Task<Result<GetPenaltyFrameworksResponse>> Handle(
        GetPenaltyFrameworksQuery request,
        CancellationToken ct)
    {
        var query = db.Set<PenaltyFramework>()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.ViolationLevel.HasValue)
            query = query.Where(p => p.ViolationLevel == request.ViolationLevel.Value);

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        query = query.OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PenaltyFrameworkItem(
                p.Id,
                p.CategoryId,
                p.Category!.NameVi,
                p.ViolationLevel.ToString(),
                p.MinAmount,
                p.MaxAmount,
                p.Currency,
                p.EffectiveFrom,
                p.EffectiveTo,
                p.IsActive,
                p.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var pagination = PaginationMeta.Create(totalCount, request.Page, request.PageSize);

        return new GetPenaltyFrameworksResponse(items, pagination);
    }
}
