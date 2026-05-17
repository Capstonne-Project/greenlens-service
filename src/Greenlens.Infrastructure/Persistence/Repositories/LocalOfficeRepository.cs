using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class LocalOfficeRepository(ApplicationDbContext db)
    : GenericRepository<LocalOffice>(db), ILocalOfficeRepository
{
    public Task<bool> ExistsByWardCodeAsync(string wardCode, CancellationToken ct = default)
        => QueryAsNoTracking()
            .AnyAsync(lo => lo.WardCode == wardCode, ct);
}
