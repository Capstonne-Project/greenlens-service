using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateDepartment;

/// <summary>
/// Admin creates a Department of Environmental Management for a Province.
/// </summary>
/// <remarks>Implements: BR-ORG-001.</remarks>
public sealed record CreateDepartmentCommand(
    string Name,
    string ProvinceCode) : IRequest<Result<CreateDepartmentResponse>>;

public sealed record CreateDepartmentResponse(Guid Id, string Name, string ProvinceCode);
