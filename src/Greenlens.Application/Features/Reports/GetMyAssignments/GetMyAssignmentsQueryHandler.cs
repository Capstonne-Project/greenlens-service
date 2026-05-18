using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
    ICurrentUser currentUser)
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
            return new GetMyAssignmentsResponse([], 0, request.Page, request.PageSize);

        var query = assignments
            .QueryAsNoTracking()
            .Include(a => a.Report)
                .ThenInclude(r => r!.Category)
            .Include(a => a.Report)
                .ThenInclude(r => r!.Media)
            .Where(a => myTeamIds.Contains(a.TeamId));

        if (request.AssignmentStatus.HasValue)
            query = query.Where(a => a.Status == request.AssignmentStatus.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

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
                    .FirstOrDefault()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetMyAssignmentsResponse(items, totalCount, request.Page, request.PageSize);
    }
}
