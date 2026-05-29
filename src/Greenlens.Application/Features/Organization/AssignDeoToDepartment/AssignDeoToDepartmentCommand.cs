using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.AssignDeoToDepartment;

/// <summary>
/// Admin assigns a DEO user to a Department (Sở TNMT cấp Tỉnh).
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed record AssignDeoToDepartmentCommand(
    Guid DepartmentId,
    Guid UserId) : IRequest<Result>;
