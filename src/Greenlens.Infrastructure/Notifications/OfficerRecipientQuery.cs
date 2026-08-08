using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Enums;
using Greenlens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Notifications;

internal sealed class OfficerRecipientQuery(ApplicationDbContext db) : IOfficerRecipientQuery
{
    public async Task<IReadOnlyList<Guid>> GetLeoIdsByOfficeAsync(Guid officeId, CancellationToken ct = default)
        => await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.LEO
                        && u.LocalOfficeId == officeId
                        && !u.IsBanned)
            .Select(u => u.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Guid>> GetDeoIdsByDepartmentAsync(
        Guid departmentId,
        CancellationToken ct = default)
        => await db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.DEO
                        && u.DepartmentId == departmentId
                        && !u.IsBanned)
            .Select(u => u.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<Guid?> GetPrimaryOfficerIdAsync(
        Guid? assignedOfficeId,
        Guid? assignedDepartmentId,
        CancellationToken ct = default)
    {
        if (assignedOfficeId.HasValue)
        {
            var leoId = await db.Users
                .AsNoTracking()
                .Where(u => u.LocalOfficeId == assignedOfficeId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (leoId != Guid.Empty)
                return leoId;
        }

        if (assignedDepartmentId.HasValue)
        {
            var deoId = await db.Users
                .AsNoTracking()
                .Where(u => u.DepartmentId == assignedDepartmentId && !u.IsBanned)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (deoId != Guid.Empty)
                return deoId;
        }

        return null;
    }
}
