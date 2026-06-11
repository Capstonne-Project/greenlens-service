using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.CreateCompanyStaff;

/// <summary>
/// CM creates a CompanyStaff account with temporary password.
/// Optionally assigns the new staff to a company team.
/// </summary>
/// <remarks>Implements: BR-CMP-004.</remarks>
public sealed record CreateCompanyStaffCommand(
    string Email,
    string FullName,
    string? Position = null,
    Guid? TeamId = null) : IRequest<Result<CreateCompanyStaffResponse>>;

public sealed record CreateCompanyStaffResponse(
    Guid UserId,
    string Email,
    string FullName,
    string TempPassword,
    Guid CompanyId,
    string? Position,
    Guid? TeamId);
