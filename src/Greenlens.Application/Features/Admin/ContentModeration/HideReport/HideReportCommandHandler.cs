using Greenlens.Application.Common.Interfaces;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Greenlens.Application.Features.Admin.ContentModeration.HideReport;

/// <summary>
/// Admin hides a report from public view. Content stays for audit 90 days.
/// </summary>
/// <remarks>Implements: BR-ADM-006.</remarks>
public sealed class HideReportCommandHandler(DbContext db, ICurrentUser currentUser)
    : IRequestHandler<HideReportCommand, Result>
{
    public async Task<Result> Handle(HideReportCommand request, CancellationToken ct)
    {
        var report = await db.Set<Report>()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, ct)
            .ConfigureAwait(false);

        if (report is null)
            return Result.Failure(new Error("Report.NotFound", "Báo cáo không tồn tại.", ErrorType.NotFound));

        if (report.IsHidden)
            return Result.Failure(new Error("Report.AlreadyHidden", "Báo cáo đã bị ẩn.", ErrorType.Conflict));

        report.Hide(currentUser.UserId, request.Reason);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
