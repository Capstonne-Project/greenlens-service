using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class InspectionEvidenceRepository(ApplicationDbContext context)
    : GenericRepository<InspectionEvidence>(context), IInspectionEvidenceRepository
{
    public async Task<IReadOnlyList<InspectionEvidence>> GetByInspectionReportIdAsync(
        Guid inspectionReportId,
        CancellationToken ct = default)
        => await Query()
            .Where(e => e.InspectionReportId == inspectionReportId)
            .OrderBy(e => e.UploadedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
