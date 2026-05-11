namespace Greenlens.Infrastructure.Persistence.Repositories.Location;

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;

internal sealed class ProvinceRepository(ApplicationDbContext db)
    : CatalogRepository<Province>(db), IProvinceRepository
{
    public Task<Province?> GetByCodeWithCatalogAsync(string code, CancellationToken ct)
        => QueryAsNoTracking()
            .Include(p => p.AdministrativeRegion)
            .Include(p => p.AdministrativeUnit)
            .FirstOrDefaultAsync(p => p.Code == code, ct);

    public async Task<IReadOnlyList<Province>> GetAllForListAsync(CancellationToken ct)
        => await QueryAsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
}
