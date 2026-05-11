using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportMediaRepository(ApplicationDbContext context)
    : GenericRepository<ReportMedia>(context), IReportMediaRepository
{
}
