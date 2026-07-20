using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReportHistory;

public sealed class GetReportHistoryQueryHandler(
    IReportStatusHistoryRepository statusHistory,
    ILogger<GetReportHistoryQueryHandler> logger)
    : IRequestHandler<GetReportHistoryQuery, Result<GetReportHistoryResponse>>
{
    public async Task<Result<GetReportHistoryResponse>> Handle(
        GetReportHistoryQuery request, CancellationToken ct)
    {
        var items = await statusHistory.QueryAsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.ReportId == request.ReportId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new StatusHistoryItem(
                h.Id, h.FromStatus, h.ToStatus,
                h.ChangedBy, h.ChangedByUser != null ? h.ChangedByUser.FullName : null,
                h.Reason, h.CreatedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Lấy lịch sử trạng thái báo cáo thành công. Số lượng: {Count}", items.Count);
        return new GetReportHistoryResponse(items);
    }
}
