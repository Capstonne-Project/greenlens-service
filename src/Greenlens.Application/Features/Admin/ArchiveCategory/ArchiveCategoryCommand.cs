using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ArchiveCategory;

/// <summary>Toggle category active/inactive (soft archive).</summary>
/// <remarks>Implements: BR-ADM-003, BR-ADM-010.</remarks>
public sealed record ArchiveCategoryCommand(Guid Id, bool Archive) : IRequest<Result>, IAuditable
{
    string IAuditable.AuditEntityType => "PollutionCategory";
    string? IAuditable.AuditEntityId => Id.ToString();
}
