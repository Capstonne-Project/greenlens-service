using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.PenaltyFrameworks.GetPenaltyFrameworks;

/// <summary>
/// Returns a paginated list of penalty framework entries with optional filters.
/// </summary>
/// <remarks>Implements: BR-ADM-008.</remarks>
public sealed class GetPenaltyFrameworksQueryHandler(IApplicationDbContext db, ILogger<GetPenaltyFrameworksQueryHandler> logger)
    : IRequestHandler<GetPenaltyFrameworksQuery, Result<GetPenaltyFrameworksResponse>>
{
    public async Task<Result<GetPenaltyFrameworksResponse>> Handle(
        GetPenaltyFrameworksQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting penalty frameworks");

        var query = db.Set<PenaltyFramework>()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (request.CategoryId.HasValue)
        {
            logger.LogInformation("CategoryId: {CategoryId}", request.CategoryId.Value);
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        }
        if (request.ViolationLevel.HasValue)
        {
            logger.LogInformation("ViolationLevel: {ViolationLevel}", request.ViolationLevel.Value);
            query = query.Where(p => p.ViolationLevel == request.ViolationLevel.Value);
        }
        if (request.IsActive.HasValue)
        {
            logger.LogInformation("IsActive: {IsActive}", request.IsActive.Value);
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }
        query = query.OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Total count: {TotalCount}", totalCount);

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

        logger.LogInformation("Penalty frameworks retrieved successfully");

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        return new GetPenaltyFrameworksResponse(items, pagination);
    }
}
