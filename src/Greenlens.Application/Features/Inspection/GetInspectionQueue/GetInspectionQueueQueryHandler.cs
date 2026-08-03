using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetInspectionQueue;

/// <summary>BR-INS-002: Inspector views inspection tasks assigned to their team within their ward.</summary>
public sealed class GetInspectionQueueQueryHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    ILogger<GetInspectionQueueQueryHandler> logger)
    : IRequestHandler<GetInspectionQueueQuery, Result<GetInspectionQueueResponse>>
{
    public async Task<Result<GetInspectionQueueResponse>> Handle(
        GetInspectionQueueQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting inspection queue");

        // Find teams the current user belongs to (Inspection type)
        var myTeams = await teamMembers.Query()
            .Where(tm => tm.UserId == currentUser.UserId)
            .Select(tm => tm.TeamId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (myTeams.Count == 0)
        {
            logger.LogWarning("No teams found for user {UserId}", currentUser.UserId);
            return Result<GetInspectionQueueResponse>.Success(
                new GetInspectionQueueResponse([], PaginationMeta.Create(request.Page, request.PageSize, 0)));
        }

        var query = inspections.QueryAsNoTracking()
            .Include(ir => ir.Report)
            .Where(ir => ir.AssignedTeamId != null && myTeams.Contains(ir.AssignedTeamId.Value));

        if (request.Status.HasValue)
        {
            logger.LogInformation("Filtering inspection queue by status {Status}", request.Status.Value);
            query = query.Where(ir => ir.Status == request.Status.Value);
        }

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
                ir.Report.Address,
                ir.Report.WardCode,
                ir.ViolatorName,
                ir.ViolationDescription,
                ir.ViolationLevel,
                ir.PenaltyAmount,
                ir.IsRepeatOffender,
                ir.SlaInspectionDueAt,
                ir.CreatedAt,
                ir.Report.Latitude,
                ir.Report.Longitude))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation("Inspection queue: {Items}", items);

        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);
        
        return new GetInspectionQueueResponse(items, pagination);
    }
}
