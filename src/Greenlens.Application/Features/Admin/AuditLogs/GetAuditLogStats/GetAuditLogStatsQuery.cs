using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.AuditLogs.GetAuditLogStats;

/// <remarks>Implements: BR-ADM-010.</remarks>
public sealed record GetAuditLogStatsQuery(
    DateTime FromDate,
    DateTime ToDate) : IRequest<Result<GetAuditLogStatsResponse>>;

public sealed record GetAuditLogStatsResponse(
    int TotalCount,
    IReadOnlyList<AuditActionCount> ByAction,
    IReadOnlyList<AuditDayCount> ByDay);

public sealed record AuditActionCount(string Action, int Count);

public sealed record AuditDayCount(DateOnly Date, int Count);
