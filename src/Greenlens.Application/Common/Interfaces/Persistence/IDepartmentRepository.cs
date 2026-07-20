using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IDepartmentRepository : IGenericRepository<Department>
{
    Task<bool> ExistsByProvinceCodeAsync(string provinceCode, CancellationToken ct = default);
}
