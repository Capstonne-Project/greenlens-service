using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class CompanyStaffRepository(ApplicationDbContext db)
    : GenericRepository<CompanyStaff>(db), ICompanyStaffRepository
{
    public async Task<CompanyStaff?> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive, ct)
            .ConfigureAwait(false);
    }
}
