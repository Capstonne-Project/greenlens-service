using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.DeleteEnvironmentalCompany;

/// <summary>
/// Soft-delete an EnvironmentalServiceCompany. 
/// Only Admin can perform this.
/// </summary>
/// <remarks>Implements: BR-ADM-010, BR-ADM-012.</remarks>
public sealed record DeleteEnvironmentalCompanyCommand(Guid Id) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "Company";
    string? IAuditable.AuditEntityId => Id.ToString();
}
