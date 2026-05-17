using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.AssignLeoToOffice;

/// <summary>
/// Admin assigns a LEO to a Local Environmental Office.
/// </summary>
/// <remarks>Implements: BR-ORG-002.</remarks>
public sealed record AssignLeoToOfficeCommand(
    Guid LocalOfficeId,
    Guid UserId) : IRequest<Result>;
