using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ToggleWasteTag;

/// <summary>Admin activates or deactivates a waste tag.</summary>
/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record ToggleWasteTagCommand(Guid Id, bool IsActive) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "WasteTag";
    string? IAuditable.AuditEntityId => Id.ToString();
}
