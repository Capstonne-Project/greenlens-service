using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DispatchToCompany;

/// <summary>
/// LEO dispatches a verified report to an EnvironmentalServiceCompany for cleanup.
/// Verified → InProgress; CompanyManager assigns specific team later.
/// </summary>
public sealed record DispatchToCompanyCommand(
    Guid ReportId,
    Guid CompanyId,
    string? Note) : IRequest<Result>;
