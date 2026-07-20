using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.SuspendCompany;

/// <summary>
/// DEO/Admin suspends a company (e.g. contract violation).
/// </summary>
/// <remarks>Implements: BR-CMP-004, BR-CMP-013, BR-ADM-010.</remarks>
public sealed record SuspendCompanyCommand(Guid CompanyId, string Reason) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Company";
    string? IAuditable.AuditEntityId => CompanyId.ToString();
}
