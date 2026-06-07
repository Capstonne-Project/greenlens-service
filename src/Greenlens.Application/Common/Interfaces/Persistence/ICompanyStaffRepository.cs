using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ICompanyStaffRepository : IGenericRepository<CompanyStaff>
{
    Task<CompanyStaff?> GetByUserIdAsync(Guid userId, CancellationToken ct);
}
