using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetCompanyAssignments;

/// <summary>
/// CompanyManager views all assignments across their company's teams.
/// Shows which team was assigned to which report, progress, and status.
/// </summary>
public sealed record GetCompanyAssignmentsQuery(
    int Page = 1,
    int PageSize = 20,
    AssignmentStatus? Status = null,
    ReportStatus? ReportStatus = null,
    string? Search = null) : IRequest<Result<GetCompanyAssignmentsResponse>>;

public sealed record GetCompanyAssignmentsResponse(
    List<CompanyAssignmentItem> Items,
    PaginationMeta Pagination);

public sealed record CompanyAssignmentItem(
    Guid AssignmentId,
    AssignmentStatus AssignmentStatus,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    string? Note,
    CompanyAssignmentReport Report,
    CompanyAssignmentTeam Team,
    string AssignedByName);

public sealed record CompanyAssignmentReport(
    Guid ReportId,
    string Code,
    string? Address,
    string? WardCode,
    string CategoryName,
    Severity Severity,
    ReportStatus Status,
    DateTime? SlaResolveDueAt);

public sealed record CompanyAssignmentTeam(
    Guid TeamId,
    string TeamName,
    int MemberCount);
