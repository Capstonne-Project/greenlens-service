using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class UserPointsRepository(ApplicationDbContext db)
    : GenericRepository<UserPoints>(db), IUserPointsRepository
{
    public async Task<UserPoints> GetOrCreateByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var local = Context.ChangeTracker.Entries<UserPoints>()
            .Select(e => e.Entity)
            .FirstOrDefault(x => x.UserId == userId);

        if (local is not null)
        {
            if (!Context.Entry(local).Collection(x => x.Transactions).IsLoaded)
                await Context.Entry(local).Collection(x => x.Transactions).LoadAsync(ct).ConfigureAwait(false);

            return local;
        }

        var existing = await Context.UserPoints
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);

        if (existing is not null)
            return existing;

        var userPoints = UserPoints.Create(userId);
        Context.UserPoints.Add(userPoints);
        return userPoints;
    }

    public async Task<UserPoints?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.UserPoints
            .AsNoTracking()
            .Include(x => x.Transactions)
            .FirstOrDefaultAsync(x => x.UserId == userId, ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasTransactionForReportAsync(
        Guid userId, Guid reportId, PointReason reason, CancellationToken ct = default)
    {
        // Ignore soft-delete filters — unique index may still see deleted rows on PostgreSQL.
        return await (
            from pt in Context.PointTransactions.IgnoreQueryFilters()
            join up in Context.UserPoints.IgnoreQueryFilters() on pt.UserPointsId equals up.Id
            where up.UserId == userId
                  && pt.ReportId == reportId
                  && pt.Reason == reason
            select pt.Id
        ).AnyAsync(ct).ConfigureAwait(false);
    }
}
