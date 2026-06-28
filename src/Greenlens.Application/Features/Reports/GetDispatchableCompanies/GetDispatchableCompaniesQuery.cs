using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetDispatchableCompanies;

/// <summary>
/// LEO retrieves companies that serve the same ward as the report,
/// so they can choose which company to dispatch the task to.
/// </summary>
/// <remarks>Implements: BR-CMP-008 (service area), BR-CMP-005 (active only).</remarks>
public sealed record GetDispatchableCompaniesQuery(Guid ReportId) : IRequest<Result<GetDispatchableCompaniesResponse>>;

public sealed record GetDispatchableCompaniesResponse(IReadOnlyList<DispatchableCompanyItem> Companies);

public sealed record DispatchableCompanyItem(
    Guid Id,
    string Name,
    string ContractNumber,
    string ContractType,
    string Status,
    string? Phone,
    string? Email,
    int ServiceAreaCount,
    int StaffCount);
