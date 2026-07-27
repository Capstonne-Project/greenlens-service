using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Notifications;

internal sealed class CompanyManagerRecipientQuery(ApplicationDbContext db) : ICompanyManagerRecipientQuery
{
    public async Task<IReadOnlyList<Guid>> GetActiveManagerIdsByCompanyAsync(
        Guid companyId,
        CancellationToken ct = default)
        => await db.CompanyStaff
            .AsNoTracking()
            .Where(cs => cs.CompanyId == companyId && cs.IsActive)
            .Join(
                db.Users.AsNoTracking().Where(u => u.Role == UserRole.CompanyManager && !u.IsBanned),
                cs => cs.UserId,
                u => u.Id,
                (cs, u) => u.Id)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
