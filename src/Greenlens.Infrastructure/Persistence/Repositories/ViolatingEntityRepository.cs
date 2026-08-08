using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class ViolatingEntityRepository(ApplicationDbContext context)
    : GenericRepository<ViolatingEntity>(context), IViolatingEntityRepository
{
    /// <inheritdoc />
    public async Task<ViolatingEntity?> FindByTaxCodeAsync(
        string taxCode, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(ve => ve.TaxCode == taxCode, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<ViolatingEntity?> FindByIdentityNumberAsync(
        string identityNumber, CancellationToken ct = default)
        => await DbSet
            .FirstOrDefaultAsync(ve => ve.IdentityNumber == identityNumber, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> TaxCodeExistsAsync(string taxCode, Guid? excludeEntityId = null, CancellationToken ct = default)
    {
        var normalized = taxCode.Trim();
        var query = Context.ViolatingEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ve => ve.TaxCode == normalized);

        if (excludeEntityId.HasValue)
            query = query.Where(ve => ve.Id != excludeEntityId.Value);

        return query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public Task<bool> IdentityNumberExistsAsync(
        string identityNumber, Guid? excludeEntityId = null, CancellationToken ct = default)
    {
        var normalized = identityNumber.Trim();
        var query = Context.ViolatingEntities
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ve => ve.IdentityNumber == normalized);

        if (excludeEntityId.HasValue)
            query = query.Where(ve => ve.Id != excludeEntityId.Value);

        return query.AnyAsync(ct);
    }

    /// <inheritdoc />
    public async Task<List<ViolatingEntity>> SearchByNameAsync(
        string name, int maxResults = 20, CancellationToken ct = default)
        => await DbSet
            .Where(ve => EF.Functions.ILike(ve.Name, $"%{name}%"))
            .OrderBy(ve => ve.Name)
            .Take(maxResults)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountInspectionsInPeriodAsync(
        Guid violatingEntityId, int months, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        return await Context.InspectionReports
            .CountAsync(ir =>
                ir.ViolatingEntityId == violatingEntityId
                && ir.Status != InspectionStatus.ClosedNoViolation
                && ir.CreatedAt >= cutoff, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> HasAnyInspectionReportsAsync(Guid violatingEntityId, CancellationToken ct = default) =>
        Context.InspectionReports
            .AsNoTracking()
            .AnyAsync(ir => ir.ViolatingEntityId == violatingEntityId, ct);
}
