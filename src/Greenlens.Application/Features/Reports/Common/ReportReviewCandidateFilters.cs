using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Reports.Common;

internal static class ReportReviewCandidateFilters
{
    public static IQueryable<Report> ApplyCommon(
        IQueryable<Report> query,
        ReportStatus? status,
        Severity? severity,
        Guid? categoryId,
        string? wardCode,
        DateTime? fromDate,
        DateTime? toDate,
        string? search)
    {
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (severity.HasValue)
            query = query.Where(r => r.Severity == severity.Value);

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(wardCode))
            query = query.Where(r => r.WardCode == wardCode);

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc));

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(r =>
                r.Code.ToLower().Contains(keyword) ||
                (r.Address != null && r.Address.ToLower().Contains(keyword)) ||
                r.Category.NameVi.ToLower().Contains(keyword) ||
                r.Category.Code.ToLower().Contains(keyword));
        }

        return query;
    }
}
