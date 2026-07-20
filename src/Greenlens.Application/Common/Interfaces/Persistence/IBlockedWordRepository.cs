using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IBlockedWordRepository : IGenericRepository<BlockedWord>
{
    Task<bool> ExistsWordAsync(string normalizedWord, Guid? excludeId, CancellationToken ct);
}
