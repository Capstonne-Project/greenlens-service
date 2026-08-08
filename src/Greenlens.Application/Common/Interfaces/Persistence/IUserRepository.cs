using System.Linq.Expressions;
using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<User, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>BR-AUTH-021: Find soft-deleted user by email (bypasses global query filter).</summary>
    Task<User?> GetDeletedByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Check email uniqueness including soft-deleted rows (DB unique index is not filtered).</summary>
    Task<bool> EmailExistsIncludingDeletedAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken ct = default);

    Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    /// <summary>Check phone uniqueness including soft-deleted rows.</summary>
    Task<bool> PhoneExistsIncludingDeletedAsync(
        string phone,
        Guid? excludeUserId = null,
        CancellationToken ct = default);
}
