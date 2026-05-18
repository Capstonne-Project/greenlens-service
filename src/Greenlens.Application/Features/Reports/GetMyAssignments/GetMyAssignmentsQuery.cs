using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyAssignments;

/// <summary>
/// Returns paginated list of reports assigned to the current user's team.
/// </summary>
/// <remarks>
/// Implements: BR-CLN-001 (Cleanup team task list), BR-INS-001 (Inspection team task list).
/// </remarks>
public sealed record GetMyAssignmentsQuery(
    int Page = 1,
    int PageSize = 20,
    AssignmentStatus? AssignmentStatus = null) : IRequest<Result<GetMyAssignmentsResponse>>;

public sealed record GetMyAssignmentsResponse(
    IReadOnlyList<MyAssignmentItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record MyAssignmentItem(
    Guid ReportId,
    string ReportCode,
    Guid AssignmentId,
    AssignmentStatus AssignmentStatus,
    string CategoryCode,
    string CategoryName,
    Severity Severity,
    ReportStatus ReportStatus,
    decimal Latitude,
    decimal Longitude,
    string? Address,
    string? WardCode,
    string? Note,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime? SlaResolveDueAt,
    string? FirstImageUrl);
