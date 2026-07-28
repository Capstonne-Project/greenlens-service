using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class CommunityCleanupEventRepository(ApplicationDbContext db)
    : GenericRepository<CommunityCleanupEvent>(db), ICommunityCleanupEventRepository
{
    public Task<CommunityCleanupEvent?> GetActiveByReportIdAsync(Guid reportId, CancellationToken ct = default)
        => Query()
            .FirstOrDefaultAsync(e => e.ReportId == reportId
                && e.Status != CommunityCleanupStatus.Completed
                && e.Status != CommunityCleanupStatus.Cancelled, ct);
}
