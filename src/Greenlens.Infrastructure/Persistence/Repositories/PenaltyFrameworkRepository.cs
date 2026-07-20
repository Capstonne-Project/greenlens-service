using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class PenaltyFrameworkRepository(ApplicationDbContext db)
    : GenericRepository<PenaltyFramework>(db), IPenaltyFrameworkRepository;

