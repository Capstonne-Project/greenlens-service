using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetMyAssignments;

/// <summary>
/// Returns reports assigned to the current user's team.
/// Looks up the user's team membership via TeamMember table, then joins with ReportAssignment.
/// </summary>
/// <remarks>
/// Implements: BR-CLN-001, BR-INS-001.
/// </remarks>
public sealed class GetMyAssignmentsQueryHandler(
    ITeamMemberRepository teamMembers,
    IReportAssignmentRepository assignments,
    ICurrentUser currentUser,
    ILogger<GetMyAssignmentsQueryHandler> logger)
    : IRequestHandler<GetMyAssignmentsQuery, Result<GetMyAssignmentsResponse>>
{
    public async Task<Result<GetMyAssignmentsResponse>> Handle(
        GetMyAssignmentsQuery request,
        CancellationToken ct)
    {
        // Find which team(s) this user belongs to
        var myTeamIds = await teamMembers
            .QueryAsNoTracking()
            .Where(m => m.UserId == currentUser.UserId)
            .Select(m => m.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (myTeamIds.Count == 0)
        {
            logger.LogWarning("Người dùng không thuộc đội nào. User ID: {UserId}", currentUser.UserId);
            return new GetMyAssignmentsResponse([], PaginationMeta.Create(request.Page, request.PageSize, 0));
        }

        var assignmentScope = assignments.QueryAsNoTracking();

        IQueryable<ReportAssignment> query = ReportAssignmentSelection.WhereLatestPerReportTeam(
                assignmentScope.Where(a => myTeamIds.Contains(a.TeamId)),
                assignmentScope)
            .Include(a => a.Report)
                .ThenInclude(r => r!.Category)
            .Include(a => a.Report)
                .ThenInclude(r => r!.Media)
            .Include(a => a.Report)
                .ThenInclude(r => r!.WasteTags)
                    .ThenInclude(wt => wt.WasteTag);

        if (request.AssignmentStatus.HasValue)
        {
            logger.LogInformation("Filtering by assignment status: {Status}", request.AssignmentStatus.Value);
            query = query.Where(a => a.Status == request.AssignmentStatus.Value);
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var items = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new MyAssignmentItem(
                a.ReportId,
                a.Report!.Code,
                a.Id,
                a.Status,
                a.Report.Category!.Code,
                a.Report.Category!.NameVi,
                a.Report.Severity,
                a.Report.Status,
                a.Report.Latitude,
                a.Report.Longitude,
                a.Report.Address,
                a.Report.WardCode,
                a.Note,
                a.AssignedAt,
                a.StartedAt,
                a.CompletedAt,
                a.Report.SlaResolveDueAt,
                a.Report.Media
                    .Where(m => m.Type == MediaType.Image)
                    .OrderBy(m => m.UploadedAt)
                    .Select(m => m.ThumbnailUrl ?? m.Url)
                    .FirstOrDefault(),
                a.Report.WasteTags
                    .Where(wt => wt.WasteTag != null)
                    .Select(wt => wt.WasteTag!.Code)
                    .ToList()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Lấy danh sách báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetMyAssignmentsResponse(items, pagination);
    }
}
