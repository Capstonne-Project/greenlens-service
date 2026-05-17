using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface ILocalOfficeRepository : IGenericRepository<LocalOffice>
{
    Task<bool> ExistsByWardCodeAsync(string wardCode, CancellationToken ct = default);
}
