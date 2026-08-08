using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Organization;

/// <summary>DEO/Admin scope checks for environmental service company read actions.</summary>
/// <remarks>Implements: BR-ADM-012, BR-CMP-021.</remarks>
internal static class CompanyAccessAuthorization
{
    public static Error? ValidateViewAccess(EnvironmentalServiceCompany company, User actor)
    {
        if (actor.Role == UserRole.Admin)
            return null;

        if (actor.Role != UserRole.DEO)
            return Errors.Auth.Forbidden;

        if (!actor.DepartmentId.HasValue)
            return Errors.Organization.DepartmentNotFound;

        if (company.DepartmentId != actor.DepartmentId)
            return Errors.Organization.CrossCompanyAccess;

        return null;
    }
}
