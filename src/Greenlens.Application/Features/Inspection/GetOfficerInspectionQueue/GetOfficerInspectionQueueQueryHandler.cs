using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Common.Models;
using Greenlens.Application.Features.Reports.GetOfficerQueue;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.GetOfficerInspectionQueue;

/// <summary>
/// Returns paginated InspectionReport queue for LEO (ward office) or DEO (department).
/// LEO: reports routed to their LocalOffice. DEO: reports in their department.
/// </summary>
/// <remarks>Implements: BR-INS-001, BR-INS-030.</remarks>
public sealed class GetOfficerInspectionQueueQueryHandler(
    IInspectionReportRepository inspections,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetOfficerInspectionQueueQueryHandler> logger)
    : IRequestHandler<GetOfficerInspectionQueueQuery, Result<GetOfficerInspectionQueueResponse>>
{
    public async Task<Result<GetOfficerInspectionQueueResponse>> Handle(
        GetOfficerInspectionQueueQuery request,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Getting officer inspection queue for user {UserId}",
            currentUser.UserId);

        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        var query = inspections.QueryAsNoTracking()
            .Include(ir => ir.Report)
            .Include(ir => ir.AssignedTeam)
            .Include(ir => ir.CreatedByOfficer)
            .AsQueryable();

        query = ApplyRoleScope(query, user);

        if (request.Status.HasValue)
            query = query.Where(ir => ir.Status == request.Status.Value);

        if (request.AssignedTeamId.HasValue)
            query = query.Where(ir => ir.AssignedTeamId == request.AssignedTeamId.Value);

        if (request.UnassignedOnly == true)
            query = query.Where(ir => ir.AssignedTeamId == null);

        if (request.FromDate.HasValue)
        {
            var from = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
            query = query.Where(ir => ir.CreatedAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
            query = query.Where(ir => ir.CreatedAt <= to);
        }

        if (request.SlaBreached == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(ir =>
                ir.SlaInspectionBreached ||
                (ir.SlaInspectionDueAt.HasValue && ir.SlaInspectionDueAt < now));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim().ToLower();
            query = query.Where(ir =>
                ir.Report!.Code.ToLower().Contains(keyword) ||
                (ir.Report.Address != null && ir.Report.Address.ToLower().Contains(keyword)) ||
                (ir.ViolatorName != null && ir.ViolatorName.ToLower().Contains(keyword)) ||
                (ir.ViolationDescription != null && ir.ViolationDescription.ToLower().Contains(keyword)));
        }

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);
        var pagination = PaginationMeta.Create(request.Page, request.PageSize, totalCount);

        var orderedQuery = ApplySort(query, request.SortBy, request.SortDir);

        var items = await orderedQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ir => new OfficerInspectionQueueItemDto(
                ir.Id,
                ir.ReportId,
                ir.Report!.Code,
                ir.Report.Status,
                ir.Status,
                ir.Report.Address,
                ir.Report.WardCode,
                ir.Report.Latitude,
                ir.Report.Longitude,
                ir.ViolatorName,
                ir.ViolationDescription,
                ir.ViolationLevel,
                ir.PenaltyAmount,
                ir.PaidAmount,
                ir.IsRepeatOffender,
                ir.AssignedTeamId,
                ir.AssignedTeam != null ? ir.AssignedTeam.Name : null,
                ir.CreatedByOfficerId,
                ir.CreatedByOfficer != null ? ir.CreatedByOfficer.FullName : null,
                ir.SlaInspectionDueAt,
                ir.SlaInspectionBreached,
                ir.PenaltyDueDate,
                ir.CreatedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Officer inspection queue returned {Count} item(s)",
            items.Count);

        return new GetOfficerInspectionQueueResponse(items, pagination);
    }

    private static IQueryable<InspectionReport> ApplyRoleScope(
        IQueryable<InspectionReport> query,
        User user)
    {
        if (user.Role == UserRole.DEO && user.DepartmentId.HasValue)
        {
            return query.Where(ir =>
                ir.Report!.AssignedDepartmentId == user.DepartmentId.Value);
        }

        if (user.Role == UserRole.LEO && user.LocalOfficeId.HasValue)
        {
            return query.Where(ir =>
                ir.Report!.AssignedOfficeId == user.LocalOfficeId.Value);
        }

        return query;
    }

    private static IOrderedQueryable<InspectionReport> ApplySort(
        IQueryable<InspectionReport> query,
        OfficerInspectionQueueSortBy sortBy,
        SortDirection sortDir)
    {
        return (sortBy, sortDir) switch
        {
            (OfficerInspectionQueueSortBy.SlaInspectionDueAt, SortDirection.Asc) =>
                query.OrderBy(ir => ir.SlaInspectionDueAt).ThenByDescending(ir => ir.CreatedAt),
            (OfficerInspectionQueueSortBy.SlaInspectionDueAt, SortDirection.Desc) =>
                query.OrderByDescending(ir => ir.SlaInspectionDueAt).ThenByDescending(ir => ir.CreatedAt),
            (OfficerInspectionQueueSortBy.Status, SortDirection.Asc) =>
                query.OrderBy(ir => ir.Status).ThenByDescending(ir => ir.CreatedAt),
            (OfficerInspectionQueueSortBy.Status, SortDirection.Desc) =>
                query.OrderByDescending(ir => ir.Status).ThenByDescending(ir => ir.CreatedAt),
            (OfficerInspectionQueueSortBy.CreatedAt, SortDirection.Asc) =>
                query.OrderBy(ir => ir.CreatedAt),
            _ => query.OrderByDescending(ir => ir.CreatedAt),
        };
    }
}
