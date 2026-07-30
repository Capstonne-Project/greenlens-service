using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.ReactivateCompany;

/// <summary>
/// DEO/Admin reactivates a suspended company.
/// </summary>
/// <remarks>Implements: BR-CMP-004, BR-ADM-010.</remarks>
public sealed record ReactivateCompanyCommand(Guid CompanyId) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Company";
    string? IAuditable.AuditEntityId => CompanyId.ToString();
}
