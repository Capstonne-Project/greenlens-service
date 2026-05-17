using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Reports.GetReportHistory;

public sealed class GetReportHistoryQueryHandler(
    IReportStatusHistoryRepository statusHistory)
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

        return new GetReportHistoryResponse(items);
    }
}
