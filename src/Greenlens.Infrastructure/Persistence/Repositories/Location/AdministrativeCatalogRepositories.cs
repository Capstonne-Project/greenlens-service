namespace Greenlens.Infrastructure.Persistence.Repositories.Location;

using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities.Location;

internal sealed class AdministrativeRegionRepository(ApplicationDbContext db)
    : CatalogRepository<AdministrativeRegion>(db), IAdministrativeRegionRepository;

internal sealed class AdministrativeUnitRepository(ApplicationDbContext db)
    : CatalogRepository<AdministrativeUnit>(db), IAdministrativeUnitRepository;
