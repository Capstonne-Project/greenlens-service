using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportProgressBoard;

/// <summary>
/// LEO dashboard: paginated card view of InProgress reports in their office.
/// </summary>
public sealed record GetReportProgressBoardQuery(
    int Page = 1,
    int PageSize = 20,
    Severity? Severity = null,
    bool SlaBreachedOnly = false) : IRequest<Result<GetReportProgressBoardResponse>>;

public sealed record GetReportProgressBoardResponse(
    IReadOnlyList<ReportProgressCardDto> Items,
    PaginationMeta Pagination);

/// <summary>Lightweight card data — enough for the board grid, no per-team detail.</summary>
public sealed record ReportProgressCardDto(
    Guid ReportId,
    string Code,
    string CategoryName,
    Severity Severity,
    string? Address,
    string? WardCode,
    DateTime? SlaResolveDueAt,
    int? HoursRemaining,
    bool IsSlaBreach,
    int TotalTeams,
    int CompletedTeams,
    int OverallProgressPercent,
    IReadOnlyList<TeamLeaderAvatarDto> TeamLeaderAvatars,
    int ExtraTeamCount);

/// <summary>Top-3 team leader avatars shown on each card.</summary>
public sealed record TeamLeaderAvatarDto(string Name, string? AvatarUrl);
