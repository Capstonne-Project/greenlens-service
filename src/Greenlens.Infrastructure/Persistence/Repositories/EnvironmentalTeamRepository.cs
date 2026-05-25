using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class EnvironmentalTeamRepository(ApplicationDbContext db)
    : GenericRepository<EnvironmentalTeam>(db), IEnvironmentalTeamRepository
{
}
