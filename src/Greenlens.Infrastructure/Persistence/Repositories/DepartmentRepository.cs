using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository(ApplicationDbContext db)
    : GenericRepository<Department>(db), IDepartmentRepository
{
    public Task<bool> ExistsByProvinceCodeAsync(string provinceCode, CancellationToken ct = default)
        => QueryAsNoTracking()
            .AnyAsync(d => d.ProvinceCode == provinceCode, ct);
}
