using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class InspectionReportRepository(ApplicationDbContext context)
    : GenericRepository<InspectionReport>(context), IInspectionReportRepository
{
    public async Task<List<InspectionReport>> GetByReportIdAsync(
        Guid reportId, CancellationToken ct = default)
        => await DbSet
            .Where(ir => ir.ReportId == reportId)
            .OrderByDescending(ir => ir.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountByViolatorInPeriodAsync(
        string violatorIdentity, int months, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        return await DbSet
            .CountAsync(ir =>
                ir.ViolatorIdentity == violatorIdentity
                && ir.Status != InspectionStatus.ClosedNoViolation
                && ir.CreatedAt >= cutoff, ct)
            .ConfigureAwait(false);
    }

    public async Task<InspectionReport?> GetByPaymentIdAsync(Guid paymentId, CancellationToken ct = default)
        => await DbSet
            .Include(ir => ir.Payments)
            .FirstOrDefaultAsync(ir => ir.Payments.Any(p => p.Id == paymentId), ct)
            .ConfigureAwait(false);

    public Task<PenaltyPayment?> FindPaymentByIdAsync(Guid paymentId, CancellationToken ct = default) =>
        Context.Set<PenaltyPayment>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
}
