using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;

/// <summary>
/// Returns all gamification point configurations (small dataset, no pagination).
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class GetGamificationConfigsQueryHandler(DbContext db)
    : IRequestHandler<GetGamificationConfigsQuery, Result<List<GamificationConfigItem>>>
{
    public async Task<Result<List<GamificationConfigItem>>> Handle(
        GetGamificationConfigsQuery request,
        CancellationToken ct)
    {
        var items = await db.Set<GamificationConfig>()
            .AsNoTracking()
            .OrderBy(c => c.ActionType)
            .Select(c => new GamificationConfigItem(
                c.Id,
                c.ActionType.ToString(),
                c.Points,
                c.Description,
                c.IsActive,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return items;
    }
}
