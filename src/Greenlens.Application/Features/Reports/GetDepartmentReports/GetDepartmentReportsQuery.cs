using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetDepartmentReports;

/// <summary>
/// Returns all reports assigned to the department managed by the current DEO.
/// Supports search, filter, sort, and pagination — parity with officer queue filters.
/// Used on the DEO Dashboard to review all reports in the province.
/// </summary>
public sealed record GetDepartmentReportsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ReportStatus? Status = null,
    Guid? CategoryId = null,
    Severity? Severity = null,
    string? WardCode = null,
    Guid? AssignedOfficeId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    bool? SlaBreached = null,
    bool? IsPossibleDuplicate = null,
    bool? IsSuspectedViolationRecurrence = null,
    bool? HasPendingReopenRequest = null,
    string? SortBy = null,
    bool SortDesc = false) : IRequest<Result<GetDepartmentReportsResponse>>;
