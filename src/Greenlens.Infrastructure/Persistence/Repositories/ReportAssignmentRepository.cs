using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportAssignmentRepository(ApplicationDbContext db)
    : GenericRepository<ReportAssignment>(db), IReportAssignmentRepository
{
    public Task<int> CountInProgressByTeamAsync(Guid teamId, CancellationToken ct = default)
        => QueryAsNoTracking()
            .CountAsync(a => a.TeamId == teamId
                && (a.Status == AssignmentStatus.Assigned || a.Status == AssignmentStatus.InProgress), ct);

    public Task<List<ReportAssignment>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default)
        => Query()
            .Where(a => a.ReportId == reportId)
            .ToListAsync(ct);

    public async Task<(List<ReportAssignment> Items, int TotalCount)> GetByTeamIdAsync(
        Guid teamId,
        AssignmentStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = QueryAsNoTracking()
            .Include(a => a.Report)
            .Where(a => a.TeamId == teamId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return (items, total);
    }
}
