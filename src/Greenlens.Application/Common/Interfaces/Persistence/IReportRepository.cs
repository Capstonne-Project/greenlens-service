using Greenlens.Domain.Entities;

namespace Greenlens.Application.Common.Interfaces.Persistence;

public interface IReportRepository : IGenericRepository<Report>
{
    /// <summary>BR-AUTH-021/022: Clear reporter link on all reports (including soft-deleted).</summary>
    Task<int> AnonymizeReporterAsync(Guid reporterId, CancellationToken ct = default);

    /// <summary>True if any report references the pollution category.</summary>
    Task<bool> ExistsByCategoryIdAsync(Guid categoryId, CancellationToken ct = default);
}
