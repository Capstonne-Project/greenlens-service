using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Organization.GetOfficeCompanies;

/// <summary>
/// LEO retrieves active companies whose service area covers
/// the ward of their assigned local office.
/// No parameters needed — uses ICurrentUser to resolve office.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area), BR-CMP-005 (active only).</remarks>
public sealed record GetOfficeCompaniesQuery : IRequest<Result<GetOfficeCompaniesResponse>>;

public sealed record GetOfficeCompaniesResponse(IReadOnlyList<OfficeCompanyItem> Companies);

public sealed record OfficeCompanyItem(
    Guid Id,
    string Name,
    string ContractNumber,
    string ContractType,
    string Status,
    string? Phone,
    string? Email,
    int ServiceAreaCount,
    int StaffCount);
