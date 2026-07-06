using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DeleteReport;

/// <summary>
/// Citizen soft-deletes a report.
/// </summary>
/// <remarks>
/// Implements: BR-REP-017 — only Submitted reports with no AI classification
/// and no officer verification can be deleted.
/// </remarks>
public sealed class DeleteReportCommandHandler(
    IReportRepository reports,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DeleteReportCommandHandler> logger) : IRequestHandler<DeleteReportCommand, Result>
{
    public async Task<Result> Handle(DeleteReportCommand request, CancellationToken ct)
    {
        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
            return Errors.Reports.ReportNotFound;

        // Only the reporter can delete their own report
        if (report.ReporterId != currentUser.UserId)
            return Errors.Reports.NotReporter;

        // BR-REP-017: Cannot delete after verified/AI processed
        if (!report.CanDelete())
            return Errors.Reports.CannotDeleteReport;

        report.SoftDelete(currentUser.UserId.ToString());
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("Report {ReportId} soft-deleted by citizen {UserId}",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}
