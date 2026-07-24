using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;

namespace Greenlens.Application.Features.Analytics.Common;

/// <summary>Resolves the caller's active company membership for company-scoped dashboard queries.</summary>
public static class CompanyContextResolver
{
    public static async Task<Result<Guid>> ResolveCompanyIdAsync(
        ICompanyStaffRepository companyStaff, Guid userId, CancellationToken ct)
    {
        var staff = await companyStaff.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
        if (staff is null || !staff.IsActive)
            return Errors.Analytics.NotCompanyStaff;

        return staff.CompanyId;
    }
}
