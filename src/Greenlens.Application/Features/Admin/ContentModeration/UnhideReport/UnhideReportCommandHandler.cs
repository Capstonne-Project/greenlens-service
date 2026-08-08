using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.ContentModeration.UnhideReport;

/// <summary>
/// Admin unhides a previously hidden report, restoring public visibility.
/// </summary>
/// <remarks>Implements: BR-ADM-006.</remarks>
public sealed class UnhideReportCommandHandler(
    IReportRepository reports,
    IUnitOfWork uow,
    ILogger<UnhideReportCommandHandler> logger)
    : IRequestHandler<UnhideReportCommand, Result>
{
    public async Task<Result> Handle(UnhideReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);

        if (report is null)
        {
            logger.LogWarning("Report not found: {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!report.IsHidden)
        {
            logger.LogWarning("Report is not hidden: {ReportId}", request.ReportId);
            return Errors.Admin.ReportNotHidden;
        }

        report.Unhide();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report unhidden successfully: {ReportId}", request.ReportId);

        return Result.Success();
    }
}
