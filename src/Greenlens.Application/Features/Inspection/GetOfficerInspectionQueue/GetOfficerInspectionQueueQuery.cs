using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Inspection.GetOfficerInspectionQueue;

/// <summary>LEO/DEO paginated inspection dossier queue scoped to office or department.</summary>
public sealed record GetOfficerInspectionQueueQuery(
    int Page = 1,
    int PageSize = 20,
    InspectionStatus? Status = null,
    Guid? AssignedTeamId = null,
    bool? UnassignedOnly = null,
    bool? SlaBreached = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null,
    OfficerInspectionQueueSortBy SortBy = OfficerInspectionQueueSortBy.CreatedAt,
    SortDirection SortDir = SortDirection.Desc) : IRequest<Result<GetOfficerInspectionQueueResponse>>;

public enum OfficerInspectionQueueSortBy
{
    CreatedAt,
    SlaInspectionDueAt,
    Status
}

public sealed record GetOfficerInspectionQueueResponse(
    IReadOnlyList<OfficerInspectionQueueItemDto> Items,
    PaginationMeta Pagination);

public sealed record OfficerInspectionQueueItemDto(
    Guid Id,
    Guid ReportId,
    string ReportCode,
    ReportStatus ReportStatus,
    InspectionStatus Status,
    string? Address,
    string? WardCode,
    decimal Latitude,
    decimal Longitude,
    string? ViolatorName,
    string? ViolationDescription,
    ViolationLevel? ViolationLevel,
    decimal? PenaltyAmount,
    decimal? PaidAmount,
    bool IsRepeatOffender,
    Guid? AssignedTeamId,
    string? AssignedTeamName,
    Guid CreatedByOfficerId,
    string? CreatedByOfficerName,
    DateTime? SlaInspectionDueAt,
    bool SlaInspectionBreached,
    DateTime? PenaltyDueDate,
    DateTime CreatedAt);
