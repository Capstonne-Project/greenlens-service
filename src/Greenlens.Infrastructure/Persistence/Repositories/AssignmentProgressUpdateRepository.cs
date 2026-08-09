using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class AssignmentProgressUpdateRepository(ApplicationDbContext context)
    : GenericRepository<AssignmentProgressUpdate>(context), IAssignmentProgressUpdateRepository;
