using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IEnvironmentalServiceCompanyRepository : IGenericRepository<EnvironmentalServiceCompany>
{
    /// <summary>Check if a company has the given ward in its service area.</summary>
    Task<bool> ServesWardAsync(Guid companyId, string wardCode, CancellationToken ct);
}
