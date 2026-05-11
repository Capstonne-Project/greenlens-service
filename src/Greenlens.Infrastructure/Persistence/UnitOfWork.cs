using Greenlens.Application.Common.Interfaces.Persistence;

namespace Greenlens.Infrastructure.Persistence;

internal sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
