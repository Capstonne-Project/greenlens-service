using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportStatusHistoryRepository(ApplicationDbContext context)
    : GenericRepository<ReportStatusHistory>(context), IReportStatusHistoryRepository
{
}
