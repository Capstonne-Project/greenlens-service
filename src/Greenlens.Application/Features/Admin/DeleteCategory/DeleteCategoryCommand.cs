using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.DeleteCategory;

/// <summary>
/// Soft-delete a PollutionCategory. 
/// Reports referencing this category will still exist, but the category won't be listed for new reports.
/// </summary>
/// <remarks>Implements: BR-ADM-003, BR-ADM-010.</remarks>
public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "PollutionCategory";
    string? IAuditable.AuditEntityId => Id.ToString();
}
