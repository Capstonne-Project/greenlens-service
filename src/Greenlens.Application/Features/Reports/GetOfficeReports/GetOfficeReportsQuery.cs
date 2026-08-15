using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetOfficeReports;

/// <summary>
/// Returns all reports assigned to the LocalOffice managed by the current LEO.
/// Includes team assignment progress for each report.
/// Supports search, filter by status/category/severity/assignmentStatus, sort, and pagination.
/// Used on the LEO Dashboard to review all reports in their ward/commune.
/// </summary>
public sealed record GetOfficeReportsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    IReadOnlyList<ReportStatus>? Statuses = null,
    Guid? CategoryId = null,
    Severity? Severity = null,
    AssignmentStatus? AssignmentStatus = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SortBy = null,
    bool SortDesc = false,
    /// <summary>Filter by active handler: company team vs community (ward) team.</summary>
    OfficeReportTeamScope TeamScope = OfficeReportTeamScope.All) : IRequest<Result<GetOfficeReportsResponse>>;
