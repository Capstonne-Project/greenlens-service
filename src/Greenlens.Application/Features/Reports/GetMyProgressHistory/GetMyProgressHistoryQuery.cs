using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyProgressHistory;

/// <summary>Returns paginated progress history for the current user's team. TeamId resolved from token.</summary>
public sealed record GetMyProgressHistoryQuery(
    int Page = 1,
    int PageSize = 20,
    AssignmentStatus? AssignmentStatus = null) : IRequest<Result<GetMyProgressHistoryResponse>>;

public sealed record GetMyProgressHistoryResponse(
    IReadOnlyList<ProgressHistoryItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ProgressHistoryItem(
    Guid ReportId,
    string ReportCode,
    Guid AssignmentId,
    AssignmentStatus AssignmentStatus,
    ReportStatus ReportStatus,
    int ProgressPercent,
    string? ProgressNote,
    DateTime? ProgressUpdatedAt,
    Guid? ProgressUpdatedByUserId,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);
