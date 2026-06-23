using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompanyManager;

/// <summary>
/// DEO creates a CompanyManager account for an existing company.
/// Can be used when a company was created without a CM (deferred onboarding),
/// or to add an additional CM to an existing company.
/// </summary>
/// <remarks>Implements: BR-CMP-001, BR-CMP-002.</remarks>
public sealed record CreateCompanyManagerCommand(
    Guid CompanyId,
    string ManagerEmail,
    string ManagerFullName) : IRequest<Result<CreateCompanyManagerResponse>>;

public sealed record CreateCompanyManagerResponse(
    Guid ManagerUserId,
    string ManagerEmail,
    string ManagerFullName,
    string TempPassword);
