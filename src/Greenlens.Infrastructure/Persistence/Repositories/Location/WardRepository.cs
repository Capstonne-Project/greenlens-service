namespace Greenlens.Infrastructure.Persistence.Repositories.Location;

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities.Location;
using Microsoft.EntityFrameworkCore;

internal sealed class WardRepository(ApplicationDbContext db)
    : CatalogRepository<Ward>(db), IWardRepository
{
    public async Task<IReadOnlyList<Ward>> GetByProvinceAsync(string provinceCode, CancellationToken ct)
        => await QueryAsNoTracking()
            .Where(w => w.ProvinceCode == provinceCode)
            .OrderBy(w => w.Name)
            .ToListAsync(ct);

    public Task<Ward?> GetByCodeWithCatalogAsync(string code, CancellationToken ct)
        => QueryAsNoTracking()
            .Include(w => w.Province)
            .Include(w => w.AdministrativeUnit)
            .FirstOrDefaultAsync(w => w.Code == code, ct);
}
