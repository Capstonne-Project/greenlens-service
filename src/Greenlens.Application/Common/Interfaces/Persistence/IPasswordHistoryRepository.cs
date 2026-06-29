using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IPasswordHistoryRepository
{
    /// <summary>Get the most recent N password hashes for a user, ordered by CreatedAt desc.</summary>
    Task<List<PasswordHistory>> GetRecentAsync(Guid userId, int count, CancellationToken ct = default);

    void Add(PasswordHistory entry);
}
