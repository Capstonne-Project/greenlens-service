using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.GamificationConfigs.GetGamificationConfigs;

/// <summary>
/// Returns all gamification point configurations (small dataset, no pagination).
/// </summary>
/// <remarks>Implements: BR-ADM-005.</remarks>
public sealed class GetGamificationConfigsQueryHandler(IApplicationDbContext db, ILogger<GetGamificationConfigsQueryHandler> logger)
    : IRequestHandler<GetGamificationConfigsQuery, Result<List<GamificationConfigItem>>>
{
    public async Task<Result<List<GamificationConfigItem>>> Handle(
        GetGamificationConfigsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting gamification configs");
        
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

        logger.LogInformation("Gamification configs retrieved successfully");

        return items;
    }
}
