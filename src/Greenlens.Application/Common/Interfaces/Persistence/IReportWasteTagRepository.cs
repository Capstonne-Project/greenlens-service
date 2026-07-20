using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IReportWasteTagRepository
{
    Task<List<ReportWasteTag>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default);
    void AddRange(IEnumerable<ReportWasteTag> entities);
    void RemoveRange(IEnumerable<ReportWasteTag> entities);
}
