using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository(ApplicationDbContext context)
    : GenericRepository<Report>(context), IReportRepository
{
}
