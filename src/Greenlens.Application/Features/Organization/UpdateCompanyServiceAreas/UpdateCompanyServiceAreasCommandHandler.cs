using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
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
    IAuditLogger auditLogger,
    ILogger<UpdateCompanyServiceAreasCommandHandler> logger)
    : IRequestHandler<UpdateCompanyServiceAreasCommand, Result>
{
    public async Task<Result> Handle(UpdateCompanyServiceAreasCommand request, CancellationToken ct)
    {
        logger.LogInformation("Updating company service areas for company {CompanyId}", request.CompanyId);

        // ── 1. Verify company exists ──
        var company = await companies.GetByIdAsync(request.CompanyId, ct).ConfigureAwait(false);
        if (company is null)
        {
            logger.LogWarning("Company not found for ID {CompanyId}", request.CompanyId);
            return Errors.Organization.CompanyNotFound;
        }

        // ── 2. Validate all ward codes exist ──
        if (request.WardCodes.Count > 0)
        {
            var existingWardCount = await wards.QueryAsNoTracking()
                .CountAsync(w => request.WardCodes.Contains(w.Code), ct)
                .ConfigureAwait(false);

            if (existingWardCount != request.WardCodes.Count)
            {
                logger.LogWarning("Ward not found for codes {WardCodes}", request.WardCodes);
                return Errors.Organization.WardNotFound;
            }
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

        var oldSnapshot = JsonSerializer.Serialize(new { wardCodes = currentWardCodes.Order().ToList() });

        // ── 5. Apply changes ──
        if (toRemove.Count > 0)
        {
            logger.LogWarning("Removing service areas for codes {WardCodes}", toRemove.Select(sa => sa.WardCode));
            serviceAreas.RemoveRange(toRemove);
        }
        if (toAdd.Count > 0)
        {
            logger.LogWarning("Adding new service areas for codes {WardCodes}", toAdd);
            var newAreas = toAdd.Select(wc => CompanyServiceArea.Create(request.CompanyId, wc));
            serviceAreas.AddRange(newAreas);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "UpdateCompanyServiceAreas",
            "Company",
            request.CompanyId.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new { wardCodes = desiredWardCodes.Order().ToList() }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "Company {CompanyId} service areas updated: +{Added} -{Removed} (total desired: {Total})",
            request.CompanyId, toAdd.Count, toRemove.Count, request.WardCodes.Count);

        return Result.Success();
    }
}
