using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository(ApplicationDbContext context)
    : GenericRepository<Report>(context), IReportRepository
{
    public async Task<int> AnonymizeReporterAsync(Guid reporterId, CancellationToken ct = default) =>
        await Context.Reports
            .IgnoreQueryFilters()
            .Where(r => r.ReporterId == reporterId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.ReporterId, (Guid?)null)
                .SetProperty(r => r.HideReporterName, true)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct)
            .ConfigureAwait(false);

    public Task<bool> ExistsByCategoryIdAsync(Guid categoryId, CancellationToken ct = default) =>
        QueryAsNoTracking().AnyAsync(r => r.CategoryId == categoryId, ct);
}
