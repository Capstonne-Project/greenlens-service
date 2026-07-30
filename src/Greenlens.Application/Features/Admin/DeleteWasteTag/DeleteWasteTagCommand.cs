using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.DeleteWasteTag;

/// <summary>
/// Soft-delete a WasteTag. 
/// Reports referencing this tag will still exist, but the tag won't be listed for new reports.
/// </summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record DeleteWasteTagCommand(Guid Id) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "WasteTag";
    string? IAuditable.AuditEntityId => Id.ToString();
}
