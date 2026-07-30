using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogStats;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed class GetAuditLogStatsQueryHandler(
    IApplicationDbContext db,
    ILogger<GetAuditLogStatsQueryHandler> logger)
    : IRequestHandler<GetAuditLogStatsQuery, Result<GetAuditLogStatsResponse>>
{
    public async Task<Result<GetAuditLogStatsResponse>> Handle(
        GetAuditLogStatsQuery request,
        CancellationToken ct)
    {
        logger.LogInformation("Getting audit log stats from {From} to {To}", request.FromDate, request.ToDate);

        var fromUtc = DateTime.SpecifyKind(request.FromDate, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(request.ToDate, DateTimeKind.Utc);

        var baseQuery = db.Set<AuditLog>()
            .AsNoTracking()
            .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt <= toUtc);

        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);

        var byAction = await baseQuery
            .GroupBy(a => a.Action)
            .Select(g => new AuditActionCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byDay = await baseQuery
            .GroupBy(a => DateOnly.FromDateTime(a.CreatedAt))
            .Select(g => new AuditDayCount(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetAuditLogStatsResponse(totalCount, byAction, byDay);
    }
}
