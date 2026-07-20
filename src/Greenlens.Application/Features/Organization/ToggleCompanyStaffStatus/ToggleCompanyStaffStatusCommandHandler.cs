using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Organization.ToggleCompanyStaffStatus;

/// <summary>
/// CM toggles a staff member's active status.
/// Validates: CM owns the company, staff belongs to same company.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed class ToggleCompanyStaffStatusCommandHandler(
    ICompanyStaffRepository companyStaffRepo,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<ToggleCompanyStaffStatusCommandHandler> logger) : IRequestHandler<ToggleCompanyStaffStatusCommand, Result>
{
    public async Task<Result> Handle(ToggleCompanyStaffStatusCommand request, CancellationToken ct)
    {
        // 1. Resolve CM's company
        var cmStaff = await companyStaffRepo.GetByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (cmStaff is null)
            return Errors.Organization.NotCompanyManager;

        // 2. Find target staff by userId within same company
        var targetStaff = await companyStaffRepo.Query()
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.CompanyId == cmStaff.CompanyId, ct)
            .ConfigureAwait(false);

        if (targetStaff is null)
            return Errors.Organization.StaffNotFound;

        // 3. Toggle status
        if (request.IsActive)
            targetStaff.Activate();
        else
            targetStaff.Deactivate();

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "CompanyStaff {UserId} status set to {IsActive} by CM {CmId}",
            request.UserId, request.IsActive, currentUser.UserId);

        return Result.Success();
    }
}
