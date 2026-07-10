using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ReportDraftRepository(ApplicationDbContext context)
    : GenericRepository<ReportDraft>(context), IReportDraftRepository
{
}
