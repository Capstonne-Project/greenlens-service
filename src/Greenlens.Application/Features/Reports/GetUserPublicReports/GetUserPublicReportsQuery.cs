using Greenlens.Application.Common.Models;
using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetUserPublicReports;

public sealed record GetUserPublicReportsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<GetUserPublicReportsResponse>>;

public sealed record GetUserPublicReportsResponse(
    IReadOnlyList<UserPublicReportItem> Items, PaginationMeta Pagination);

/// <summary>
/// Báo cáo công khai trên hồ sơ người dùng khác. Không có `Address` chi tiết
/// để tránh lộ nơi ở/lui tới thường xuyên của người gửi (BR-DAT-002).
/// </summary>
public sealed record UserPublicReportItem(
    Guid Id, string Code, string CategoryName,
    Severity Severity, ReportStatus Status,
    DateTime CreatedAt, string? ImageUrl);
