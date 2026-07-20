using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.UpdateCompanyServiceAreas;

/// <summary>
/// Replaces the full set of wards for a company's service area (diff-based: add missing, remove stale).
/// </summary>
/// <remarks>Implements: BR-CMP-008, BR-CMP-014.</remarks>
public sealed class UpdateCompanyServiceAreasCommandHandler(
    IEnvironmentalServiceCompanyRepository companies,
    ICompanyServiceAreaRepository serviceAreas,
    IWardRepository wards,
    IUnitOfWork uow,
    ILogger<UpdateCompanyServiceAreasCommandHandler> logger)
    : IRequestHandler<UpdateCompanyServiceAreasCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyServiceAreasCommand request, CancellationToken ct)
    {
        // ── 1. Verify company exists ──
        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
            return Errors.Organization.CompanyNotFound;

        // ── 2. Validate all ward codes exist ──
        if (request.WardCodes.Count > 0)
        {
            var existingWardCount = await wards.QueryAsNoTracking()
                .CountAsync(w => request.WardCodes.Contains(w.Code), ct)
                .ConfigureAwait(false);

            if (existingWardCount != request.WardCodes.Count)
                return Errors.Organization.WardNotFound;
        }

        // ── 3. Load current service areas for this company ──
        var currentAreas = await serviceAreas.QueryAsNoTracking()
            .Where(sa => sa.CompanyId == request.CompanyId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var currentWardCodes = currentAreas.Select(sa => sa.WardCode).ToHashSet();
        var desiredWardCodes = request.WardCodes.ToHashSet();

        // ── 4. Diff: find what to add and what to remove ──
        var toAdd = desiredWardCodes.Except(currentWardCodes).ToList();
        var toRemove = currentAreas.Where(sa => !desiredWardCodes.Contains(sa.WardCode)).ToList();

        // ── 5. Apply changes ──
        if (toRemove.Count > 0)
            serviceAreas.RemoveRange(toRemove);

        if (toAdd.Count > 0)
        {
            var newAreas = toAdd.Select(wc => CompanyServiceArea.Create(request.CompanyId, wc));
            serviceAreas.AddRange(newAreas);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company {CompanyId} service areas updated: +{Added} -{Removed} (total desired: {Total})",
            request.CompanyId, toAdd.Count, toRemove.Count, request.WardCodes.Count);

        return Result.Success();
    }
}
