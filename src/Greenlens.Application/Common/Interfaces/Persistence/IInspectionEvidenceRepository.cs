using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IInspectionEvidenceRepository : IGenericRepository<InspectionEvidence>
{
    Task<IReadOnlyList<InspectionEvidence>> GetByInspectionReportIdAsync(
        Guid inspectionReportId,
        CancellationToken ct = default);
}
