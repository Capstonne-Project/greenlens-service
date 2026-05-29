using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetOfficerQueue;

/// <summary>
/// Returns paginated queue of reports for the current officer's area.
/// DEO sees all reports in their department (Submitted/Verified — needs verify or dispatch).
/// LEO sees only Dispatched reports for their office (needs team assignment).
/// Sorted by priority score descending (BR-OFF-010).
/// </summary>
public sealed class GetOfficerQueueQueryHandler(
    IReportRepository reports,
    IUserRepository users,
    ICurrentUser currentUser) : IRequestHandler<GetOfficerQueueQuery, Result<GetOfficerQueueResponse>>
{
    public async Task<Result<GetOfficerQueueResponse>> Handle(
        GetOfficerQueueQuery request,
        CancellationToken ct)
    {
        var user = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
        if (user is null)
            return Errors.Users.UserNotFound;

        var query = reports.QueryAsNoTracking()
            .Include(r => r.Category)
            .AsQueryable();

        // ── Role-based scope filtering ──
        if (user.Role == UserRole.DEO && user.DepartmentId.HasValue)
        {
            // DEO sees all reports in their department queue (province level)
            query = query.Where(r => r.AssignedDepartmentId == user.DepartmentId.Value);
        }
        else if (user.Role == UserRole.LEO && user.LocalOfficeId.HasValue)
        {
            // LEO sees only Dispatched reports assigned to their office
            query = query.Where(r =>
                r.AssignedOfficeId == user.LocalOfficeId.Value &&
                r.Status == ReportStatus.Dispatched);
        }

        // Status filter (override role-based default)
        if (request.StatusFilter.HasValue)
            query = query.Where(r => r.Status == request.StatusFilter.Value);

        var totalCount = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.PriorityScore)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new OfficerQueueItem(
                r.Id,
                r.Code,
                r.Category.Code,
                r.Category.NameVi,
                r.Severity,
                r.Status,
                r.Latitude,
                r.Longitude,
                r.Address,
                r.WardCode,
                r.PriorityScore,
                r.CreatedAt,
                r.SlaVerifyDueAt,
                r.SlaResolveDueAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GetOfficerQueueResponse(items, totalCount, request.Page, request.PageSize);
    }
}
