using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportWasteTagRepository(ApplicationDbContext context)
    : IReportWasteTagRepository
{
    private readonly DbSet<ReportWasteTag> _dbSet = context.Set<ReportWasteTag>();

    public async Task<List<ReportWasteTag>> GetByReportIdAsync(Guid reportId, CancellationToken ct = default) =>
        await _dbSet
            .Where(rt => rt.ReportId == reportId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public void AddRange(IEnumerable<ReportWasteTag> entities) => _dbSet.AddRange(entities);

    public void RemoveRange(IEnumerable<ReportWasteTag> entities) => _dbSet.RemoveRange(entities);
}
