using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Application.Features.Reports.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.GetReportHistory;

public sealed class GetReportHistoryQueryHandler(
    IReportRepository reports,
    IReportStatusHistoryRepository statusHistory,
    IUserRepository users,
    ICurrentUser currentUser,
    ILogger<GetReportHistoryQueryHandler> logger)
    : IRequestHandler<GetReportHistoryQuery, Result<GetReportHistoryResponse>>
{
    public async Task<Result<GetReportHistoryResponse>> Handle(
        GetReportHistoryQuery request, CancellationToken ct)
    {
        logger.LogInformation("Getting report history for report {ReportId}", request.ReportId);

        if (currentUser.IsAuthenticated && currentUser.Role is "LEO" or "DEO")
        {
            var actor = await users.GetByIdAsync(currentUser.UserId, ct).ConfigureAwait(false);
            if (actor is null)
                return Errors.Users.UserNotFound;

            var report = await reports.QueryAsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
                .ConfigureAwait(false);

            if (report is null)
                return Errors.Reports.ReportNotFound;

            var accessError = ReportReviewCandidateFilters.ValidateReportAccess(
                report, actor, currentUser.Role);
            if (accessError is not null)
            {
                logger.LogWarning(
                    "User {UserId} denied history for report {ReportId}: {ErrorCode}",
                    currentUser.UserId, request.ReportId, accessError.Code);
                return accessError;
            }
        }

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
