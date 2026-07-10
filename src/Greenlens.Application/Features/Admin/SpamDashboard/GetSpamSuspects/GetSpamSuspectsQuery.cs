using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.SpamDashboard.GetSpamSuspects;

/// <summary>
/// Returns a list of users flagged as potential spam by heuristic rules + AI flags.
/// </summary>
/// <remarks>
/// Implements: BR-ADM-007.
/// Heuristic rules:
///   1. Submit ≥ 5 reports/hour
///   2. ≥ 3 rejected reports in last 7 days
///   3. ≥ 2 reports flagged as IrrelevantOrSuspectedAbusive by AI
/// </remarks>
public sealed record GetSpamSuspectsQuery(
    int Page = 1,
    int PageSize = 20,
    int MinReportsPerHour = 5,
    int MinRejected7Days = 3,
    int MinAiFlagged = 2) : IRequest<Result<GetSpamSuspectsResponse>>;

public sealed record GetSpamSuspectsResponse(
    List<SpamSuspectItem> Items,
    int TotalCount);

public sealed record SpamSuspectItem(
    Guid UserId,
    string FullName,
    string Email,
    bool IsBanned,
    int ReportsLastHour,
    int RejectedLast7Days,
    int AiFlaggedCount,
    string SuspectReasons);
