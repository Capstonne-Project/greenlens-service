using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IReportAssignmentRepository : IGenericRepository<ReportAssignment>
{
    Task<int> CountInProgressByTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<List<ReportAssignment>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);
    Task<(List<ReportAssignment> Items, int TotalCount)> GetByTeamIdAsync(
        Guid teamId,
        AssignmentStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>BR-CMP-013: Get all active (Assigned/InProgress) assignments for teams of a company.</summary>
    Task<List<ReportAssignment>> GetActiveByCompanyTeamsAsync(Guid companyId, CancellationToken ct = default);
}
