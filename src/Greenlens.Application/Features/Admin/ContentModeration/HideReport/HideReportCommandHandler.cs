using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
namespace Greenlens.Application.Features.Admin.ContentModeration.HideReport;

/// <summary>
/// Admin hides a report from public view. Content stays for audit 90 days.
/// </summary>
/// <remarks>Implements: BR-ADM-006.</remarks>
public sealed class HideReportCommandHandler(
    IReportRepository reports,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<HideReportCommandHandler> logger)
    : IRequestHandler<HideReportCommand, Result>
{
    public async Task<Result> Handle(HideReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found: {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (report.IsHidden)
        {
            logger.LogWarning("Report is already hidden: {ReportId}", request.ReportId);
            return Result.Failure(Errors.Admin.ReportAlreadyHidden);
        }

        logger.LogInformation("Hiding report: {ReportId}", request.ReportId);
        report.Hide(currentUser.UserId, request.Reason);
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report hidden successfully: {ReportId}", request.ReportId);

        return Result.Success();
    }
}
