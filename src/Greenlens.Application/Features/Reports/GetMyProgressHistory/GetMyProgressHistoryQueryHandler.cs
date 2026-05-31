using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyProgressHistory;

/// <summary>
/// Returns paginated progress history for the current user's team. TeamId resolved from token.
/// </summary>
public sealed class GetMyProgressHistoryQueryHandler(
    IReportAssignmentRepository assignments,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    ILogger<GetMyProgressHistoryQueryHandler> logger) : IRequestHandler<GetMyProgressHistoryQuery, Result<GetMyProgressHistoryResponse>>
{
    public async Task<Result<GetMyProgressHistoryResponse>> Handle(
        GetMyProgressHistoryQuery request, CancellationToken ct)
    {
        var leader = await teamMembers.GetLeaderByUserIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (leader is null)
        {
            logger.LogWarning("Người dùng không phải là đội trưởng. User ID: {UserId}", currentUser.UserId);
            return Errors.Reports.NotTeamLeader;
        }

        var (items, total) = await assignments.GetByTeamIdAsync(
            leader.TeamId,
            request.AssignmentStatus,
            request.Page,
            request.PageSize,
            ct).ConfigureAwait(false);

        var result = items.Select(a => new ProgressHistoryItem(
            ReportId: a.ReportId,
            ReportCode: a.Report?.Code ?? string.Empty,
            AssignmentId: a.Id,
            AssignmentStatus: a.Status,
            ReportStatus: a.Report?.Status ?? default,
            ProgressPercent: a.ProgressPercent,
            ProgressNote: a.ProgressNote,
            ProgressUpdatedAt: a.ProgressUpdatedAt,
            ProgressUpdatedByUserId: a.ProgressUpdatedByUserId,
            AssignedAt: a.AssignedAt,
            StartedAt: a.StartedAt,
            CompletedAt: a.CompletedAt
        )).ToList();

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, total);

        logger.LogInformation("Lấy lịch sử tiến độ thành công. Số lượng: {Count}", result.Count);
        return new GetMyProgressHistoryResponse(result, pagination);
    }
}
