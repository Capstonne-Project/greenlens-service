namespace Greenlens.Application.Common.Models;

public sealed record PagedList<T>(
    List<T> Items,
    PaginationMeta Pagination);

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrev)
{
    public static PaginationMeta Create(int page, int pageSize, int totalItems)
    {
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PaginationMeta(
            page,
            pageSize,
            totalItems,
            totalPages,
            HasNext: page < totalPages,
            HasPrev: page > 1);
    }
}
