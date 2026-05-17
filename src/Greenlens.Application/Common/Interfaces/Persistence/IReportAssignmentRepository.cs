using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IReportAssignmentRepository : IGenericRepository<ReportAssignment>
{
    Task<int> CountInProgressByTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<List<ReportAssignment>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);
}
