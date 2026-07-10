using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ContentModeration.HideReport;

/// <summary>
/// Admin hides a report from public view. Content stays for audit 90 days.
/// </summary>
/// <remarks>Implements: BR-ADM-006.</remarks>
public sealed class HideReportCommandHandler(
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser)
    : IRequestHandler<HideReportCommand, Result>
{
    public async Task<Result> Handle(HideReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);

        if (report is null)
            return Result.Failure(new Error("Report.NotFound", "Báo cáo không tồn tại.", ErrorType.NotFound));

        if (report.IsHidden)
            return Result.Failure(new Error("Report.AlreadyHidden", "Báo cáo đã bị ẩn.", ErrorType.Conflict));

        report.Hide(currentUser.UserId, request.Reason);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
