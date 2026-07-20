using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Gamification.GetMyPoints;

/// <remarks>Implements: BR-GAM-001, BR-GAM-003.</remarks>
public sealed record GetMyPointsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<MyPointsResponse>>;

public sealed record MyPointsResponse(
    int TotalPoints,
    int Level,
    bool IsLocked,
    DateTime? LockedUntil,
    IReadOnlyList<PointTransactionItem> RecentTransactions,
    int TotalTransactions);

public sealed record PointTransactionItem(
    Guid Id, int Points, PointReason Reason,
    Guid? ReportId, DateTime CreatedAt);

public sealed class GetMyPointsQueryHandler(
    IUserPointsRepository userPointsRepo)
    : IRequestHandler<GetMyPointsQuery, Result<MyPointsResponse>>
{
    public async Task<Result<MyPointsResponse>> Handle(
        GetMyPointsQuery request, CancellationToken ct)
    {
        var up = await userPointsRepo.QueryAsNoTracking()
            .Where(x => x.UserId == request.UserId)
            .Select(x => new
            {
                x.TotalPoints,
                x.IsLocked,
                x.LockedUntil,
                TotalTx = x.Transactions.Count(),
                Transactions = x.Transactions
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(t => new PointTransactionItem(
                        t.Id, t.Points, t.Reason, t.ReportId, t.CreatedAt))
                    .ToList()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (up is null)
        {
            // User has no gamification record yet — return defaults
            return new MyPointsResponse(0, 1, false, null, [], 0);
        }

        var level = up.TotalPoints switch
        {
            >= 5000 => 5,
            >= 1500 => 4,
            >= 500 => 3,
            >= 100 => 2,
            _ => 1
        };

        return new MyPointsResponse(
            up.TotalPoints, level, up.IsLocked, up.LockedUntil,
            up.Transactions, up.TotalTx);
    }
}
