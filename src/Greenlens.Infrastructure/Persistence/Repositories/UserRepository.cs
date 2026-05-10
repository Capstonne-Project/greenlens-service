using System.Linq.Expressions;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ApplicationDbContext context)
    : GenericRepository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct)
            .ConfigureAwait(false);

    public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(u => u.GoogleId == googleId, ct)
            .ConfigureAwait(false);

    public async Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? await DbSet.CountAsync(ct).ConfigureAwait(false)
            : await DbSet.CountAsync(predicate, ct).ConfigureAwait(false);
}
