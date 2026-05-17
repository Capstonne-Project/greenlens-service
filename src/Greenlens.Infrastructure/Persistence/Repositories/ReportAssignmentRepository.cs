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
}
