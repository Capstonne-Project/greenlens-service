using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IInspectionReportRepository : IGenericRepository<InspectionReport>
{
    Task<List<InspectionReport>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>BR-INS-022: Count inspection reports for same violator identity within a period.</summary>
    Task<int> CountByViolatorInPeriodAsync(string violatorIdentity, int months, CancellationToken ct = default);
}
