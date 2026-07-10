using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Admin.ContentModeration.UnhideReport;

/// <summary>
/// Admin unhides a previously hidden report, restoring public visibility.
/// </summary>
/// <remarks>Implements: BR-ADM-006.</remarks>
public sealed class UnhideReportCommandHandler(
    IReportRepository reports,
    IUnitOfWork uow)
    : IRequestHandler<UnhideReportCommand, Result>
{
    public async Task<Result> Handle(UnhideReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);

        if (report is null)
            return Result.Failure(new Error("Report.NotFound", "Báo cáo không tồn tại.", ErrorType.NotFound));

        if (!report.IsHidden)
            return Result.Failure(new Error("Report.NotHidden", "Báo cáo không bị ẩn.", ErrorType.Conflict));

        report.Unhide();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }
}
