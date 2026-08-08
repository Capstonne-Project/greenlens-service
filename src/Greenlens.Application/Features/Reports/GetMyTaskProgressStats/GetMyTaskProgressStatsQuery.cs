using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetMyTaskProgressStats;

/// <summary>
/// Aggregated progress stats (status/severity distribution, overdue count, 30-day completion trend)
/// for the current user's team assignments — backs the mobile "Tiến độ" dashboard.
/// </summary>
public sealed record GetMyTaskProgressStatsQuery() : IRequest<Result<MyTaskProgressStatsResponse>>;

public sealed record StatusCountItem(AssignmentStatus Status, int Count);

public sealed record SeverityCountItem(Severity Severity, int Count);

public sealed record DailyCompletionItem(DateOnly Date, int Count);

public sealed record MyTaskProgressStatsResponse(
    int TotalCount,
    List<StatusCountItem> StatusCounts,
    List<SeverityCountItem> SeverityCounts,
    int OverdueCount,
    List<DailyCompletionItem> CompletionTrend);
