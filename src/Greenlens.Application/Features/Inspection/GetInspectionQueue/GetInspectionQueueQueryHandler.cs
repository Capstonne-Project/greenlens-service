using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Inspection.GetInspectionQueue;

/// <summary>BR-INS-002: Inspector views inspection tasks assigned to their team within their ward.</summary>
public sealed class GetInspectionQueueQueryHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser)
    : IRequestHandler<GetInspectionQueueQuery, Result<GetInspectionQueueResponse>>
{
    public async Task<Result<GetInspectionQueueResponse>> Handle(
        GetInspectionQueueQuery request, CancellationToken ct)
    {
        // Find teams the current user belongs to (Inspection type)
        var myTeams = await teamMembers.Query()
            .Where(tm => tm.UserId == currentUser.UserId)
            .Select(tm => tm.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (myTeams.Count == 0)
            return Result<GetInspectionQueueResponse>.Success(
                new GetInspectionQueueResponse([], 0, request.Page, request.PageSize));

        var query = inspections.QueryAsNoTracking()
            .Include(ir => ir.Report)
            .Where(ir => ir.AssignedTeamId != null && myTeams.Contains(ir.AssignedTeamId.Value));

        if (request.Status.HasValue)
            query = query.Where(ir => ir.Status == request.Status.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(ir => ir.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ir => new InspectionQueueItemDto(
                ir.Id,
                ir.ReportId,
                ir.Report!.Code,
                ir.Status,
                ir.ViolatorName,
                ir.ViolationDescription,
                ir.ViolationLevel,
                ir.PenaltyAmount,
                ir.IsRepeatOffender,
                ir.SlaInspectionDueAt,
                ir.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetInspectionQueueResponse(items, totalCount, request.Page, request.PageSize);
    }
}
