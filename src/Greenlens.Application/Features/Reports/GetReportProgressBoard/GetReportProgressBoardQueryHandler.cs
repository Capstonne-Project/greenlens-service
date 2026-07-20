using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReportProgressBoard;

/// <summary>
/// Returns paginated card view of InProgress reports scoped to the LEO's office.
/// Each card includes SLA countdown, overall progress %, and top-3 team leader avatars.
/// </summary>
/// <remarks>
/// Implements: BR-OFF-010 (priority sort), BR-OFF-020 (SLA tracking).
/// </remarks>
public sealed class GetReportProgressBoardQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetReportProgressBoardQueryHandler> logger)
    : IRequestHandler<GetReportProgressBoardQuery, Result<GetReportProgressBoardResponse>>
{
    private const int MaxAvatarsPerCard = 3;

    public async Task<Result<GetReportProgressBoardResponse>> Handle(
        GetReportProgressBoardQuery request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        if (!user.LocalOfficeId.HasValue)
            return Errors.Users.UserNotFound; // LEO must be linked to an office

        // ── Base query: InProgress reports in this LEO's office ───
        var baseQuery = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .Include(r => r.Assignments)
                .ThenInclude(a => a.Team)
                    .ThenInclude(t => t!.Members.Where(m => m.IsLeader))
                        .ThenInclude(m => m.User)
            .Where(r =>
                r.AssignedOfficeId == user.LocalOfficeId.Value &&
                r.Status == ReportStatus.InProgress);

        // ── Optional filters ──────────────────────────────────────
        if (request.Severity.HasValue)
            baseQuery = baseQuery.Where(r => r.Severity == request.Severity.Value);

        if (request.SlaBreachedOnly)
            baseQuery = baseQuery.Where(r =>
                r.SlaResolveDueAt.HasValue &&
                r.SlaResolveDueAt.Value < DateTime.UtcNow);

        // ── Pagination ────────────────────────────────────────────
        var totalCount = await baseQuery.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var pageItems = await baseQuery
            .OrderByDescending(r => r.PriorityScore)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;

        // ── Map to card DTOs ──────────────────────────────────────
        var cards = pageItems.Select(r =>
        {
            var allAssignments = r.Assignments.ToList();
            var activeAssignments = allAssignments
                .Where(a => a.Status != AssignmentStatus.Declined)
                .ToList();

            int overallPercent = activeAssignments.Count > 0
                ? (int)activeAssignments.Average(a =>
                    a.Status == AssignmentStatus.Completed ? 100 : a.ProgressPercent)
                : 0;

            // Top 3 leader avatars (one per team, ordered by assignment date)
            var leaders = allAssignments
                .Where(a => a.Status != AssignmentStatus.Declined)
                .OrderBy(a => a.AssignedAt)
                .Select(a => a.Team?.Members.FirstOrDefault(m => m.IsLeader)?.User)
                .Where(u => u is not null)
                .Take(MaxAvatarsPerCard)
                .Select(u => new TeamLeaderAvatarDto(u!.FullName, u.AvatarUrl))
                .ToList();

            int extraTeamCount = Math.Max(0, activeAssignments.Count - MaxAvatarsPerCard);

            int? hoursRemaining = r.SlaResolveDueAt.HasValue
                ? (int)(r.SlaResolveDueAt.Value - now).TotalHours
                : null;

            return new ReportProgressCardDto(
                r.Id,
                r.Code,
                r.Category.NameVi,
                r.Severity,
                r.Address,
                r.WardCode,
                r.SlaResolveDueAt,
                hoursRemaining,
                IsSlaBreach: hoursRemaining.HasValue && hoursRemaining.Value < 0,
                TotalTeams: allAssignments.Count,
                CompletedTeams: allAssignments.Count(a => a.Status == AssignmentStatus.Completed),
                OverallProgressPercent: overallPercent,
                TeamLeaderAvatars: leaders,
                ExtraTeamCount: extraTeamCount);
        }).ToList();

        logger.LogInformation(
            "LEO {UserId} fetched progress board — {Count}/{Total} reports",
            currentUser.UserId, cards.Count, totalCount);

        return new GetReportProgressBoardResponse(cards, pagination);
    }
}
