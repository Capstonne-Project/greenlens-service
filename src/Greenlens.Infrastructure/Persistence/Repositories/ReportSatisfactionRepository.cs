using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportSatisfactionRepository(ApplicationDbContext context)
    : GenericRepository<ReportSatisfaction>(context), IReportSatisfactionRepository
{
}
