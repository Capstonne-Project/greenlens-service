using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReports;

public sealed record GetReportsQuery(
    int Page = 1, int PageSize = 20,
    ReportStatus? Status = null,
    Guid? CategoryId = null,
    string? WardCode = null,
    Severity? Severity = null,
    /// <summary>Tìm theo mã báo cáo, mô tả, hoặc địa chỉ. Không phân biệt hoa/thường.</summary>
    string? Keyword = null) : IRequest<Result<GetReportsResponse>>;

public sealed record GetReportsResponse(
    IReadOnlyList<ReportListItem> Items, PaginationMeta Pagination);

public sealed record ReportListItem(
    Guid Id, string Code, string CategoryCode, string CategoryName,
    Severity Severity, ReportStatus Status, decimal Latitude, decimal Longitude,
    string? Address, string? WardCode, int ReporterCount,
    DateTime CreatedAt, DateTime? ResolvedAt);
