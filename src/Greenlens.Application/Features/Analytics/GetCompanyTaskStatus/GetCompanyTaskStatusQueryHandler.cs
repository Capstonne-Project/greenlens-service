using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyTaskStatus;

/// <summary>Distribution of assignment statuses across the caller's company teams.</summary>
public sealed class GetCompanyTaskStatusQueryHandler(
    IReportAssignmentRepository assignments,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyTaskStatusQuery, Result<List<TaskStatusItem>>>
{
    public async Task<Result<List<TaskStatusItem>>> Handle(
        GetCompanyTaskStatusQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);

        var counts = await assignments.QueryAsNoTracking()
            .Where(a => a.Team!.CompanyId == companyId
                        && a.AssignedAt >= from && a.AssignedAt <= to)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var total = counts.Sum(c => c.Count);

        var result = counts
            .Select(c => new TaskStatusItem(
                c.Status,
                c.Count,
                total == 0 ? 0m : Math.Round(100m * c.Count / total, 1)))
            .OrderByDescending(i => i.Count)
            .ToList();

        return result;
    }
}
