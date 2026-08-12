using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.GetReportHistory;

public sealed record GetReportHistoryQuery(Guid ReportId) : IRequest<Result<GetReportHistoryResponse>>;

public sealed record GetReportHistoryResponse(IReadOnlyList<StatusHistoryItem> Items);

public sealed record StatusHistoryItem(
    Guid Id, ReportStatus? FromStatus, ReportStatus ToStatus,
    Guid? ChangedBy, string? ChangedByName,
    string? Reason, DateTime CreatedAt,
    /// <summary>
    /// JSON tuỳ chọn phân loại event khi FromStatus == ToStatus (không có chuyển trạng thái
    /// thật) — ví dụ {"eventType":"ReopenRequested"|"ReopenRejected"}. FE dùng để render
    /// đúng nhánh timeline mà không cần suy luận qua so khớp text `Reason`.
    /// </summary>
    string? Metadata);
