using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class EnvironmentalServiceCompanyRepository(ApplicationDbContext db)
    : GenericRepository<EnvironmentalServiceCompany>(db), IEnvironmentalServiceCompanyRepository
{
}
