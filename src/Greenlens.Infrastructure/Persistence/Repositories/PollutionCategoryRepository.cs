using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class PollutionCategoryRepository(ApplicationDbContext context)
    : GenericRepository<PollutionCategory>(context), IPollutionCategoryRepository
{
    public Task<bool> ExistsActiveAsync(Guid id, CancellationToken ct = default) =>
        DbSet.AnyAsync(c => c.Id == id && c.IsActive, ct);
}
