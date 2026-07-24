using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Analytics.Common;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Analytics.GetCompanyQueueAging;

/// <summary>Age distribution of the caller's company tasks still open (dispatched but not resolved).</summary>
public sealed class GetCompanyQueueAgingQueryHandler(
    IReportRepository reports,
    ICompanyStaffRepository companyStaff,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : IRequestHandler<GetCompanyQueueAgingQuery, Result<List<CompanyQueueAgingBucket>>>
{
    private static readonly ReportStatus[] OpenStatuses =
        [ReportStatus.Verified, ReportStatus.InProgress];

    public async Task<Result<List<CompanyQueueAgingBucket>>> Handle(
        GetCompanyQueueAgingQuery request, CancellationToken ct)
    {
        var companyIdResult = await CompanyContextResolver
            .ResolveCompanyIdAsync(companyStaff, currentUser.UserId, ct)
            .ConfigureAwait(false);
        if (companyIdResult.IsFailure)
            return companyIdResult.Error!;

        var companyId = companyIdResult.Value;
        var (from, to) = DateRangeDefaults.Resolve(request.From, request.To, clock.UtcNow);
        var now = clock.UtcNow;

        var dispatchedAtList = await reports.QueryAsNoTracking()
            .Where(r => r.AssignedCompanyId == companyId
                        && OpenStatuses.Contains(r.Status)
                        && r.DispatchedToCompanyAt >= from && r.DispatchedToCompanyAt <= to)
            .Select(r => r.DispatchedToCompanyAt!.Value)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var buckets = new (string Range, Func<double, bool> Match)[]
        {
            ("0-6h", h => h < 6),
            ("6-24h", h => h is >= 6 and < 24),
            ("24-72h", h => h is >= 24 and < 72),
            (">72h", h => h >= 72)
        };

        var ageHours = dispatchedAtList.Select(d => (now - d).TotalHours).ToList();

        var result = buckets
            .Select(b => new CompanyQueueAgingBucket(b.Range, ageHours.Count(h => b.Match(h))))
            .ToList();

        return result;
    }
}
