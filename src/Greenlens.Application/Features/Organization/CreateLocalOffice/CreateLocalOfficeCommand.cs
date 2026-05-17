using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateLocalOffice;

/// <summary>
/// Admin onboards a new ward/commune Local Environmental Office.
/// </summary>
/// <remarks>Implements: BR-ORG-002, BR-ADM-011.</remarks>
public sealed record CreateLocalOfficeCommand(
    string Name,
    Guid DepartmentId,
    string WardCode,
    Guid? OfficerId) : IRequest<Result<CreateLocalOfficeResponse>>;

public sealed record CreateLocalOfficeResponse(Guid Id, string Name, Guid DepartmentId, string WardCode);
