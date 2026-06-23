using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class EnvironmentalServiceCompanyRepository(ApplicationDbContext db)
    : GenericRepository<EnvironmentalServiceCompany>(db), IEnvironmentalServiceCompanyRepository
{
    /// <inheritdoc />
    public async Task<bool> ServesWardAsync(Guid companyId, string wardCode, CancellationToken ct)
    {
        return await db.CompanyServiceAreas
            .AnyAsync(sa => sa.CompanyId == companyId && sa.WardCode == wardCode, ct)
            .ConfigureAwait(false);
    }
}
