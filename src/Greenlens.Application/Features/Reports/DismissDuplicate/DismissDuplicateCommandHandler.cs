using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.DismissDuplicate;

/// <summary>
/// LEO clears a possible-duplicate flag after reviewing (the report is not a duplicate).
/// </summary>
/// <remarks>Implements: BR-REP-031 (LEO makes the final duplicate decision).</remarks>
public sealed class DismissDuplicateCommandHandler(
    IReportRepository reports,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<DismissDuplicateCommandHandler> logger) : IRequestHandler<DismissDuplicateCommand, Result>
{
    public async Task<Result> Handle(DismissDuplicateCommand request, CancellationToken ct)
    {
        logger.LogInformation("Dismissing duplicate for report {ReportId}", request.ReportId);

        var report = await reports.GetByIdAsync(request.ReportId, ct).ConfigureAwait(false);
        if (report is null)
        {
            logger.LogWarning("Report not found for ID {ReportId}", request.ReportId);
            return Errors.Reports.ReportNotFound;
        }

        if (!report.IsPossibleDuplicate)
        {
            logger.LogWarning("Report {ReportId} is not a possible duplicate", request.ReportId);
            return Errors.Reports.NotPossibleDuplicate;
        }

        report.DismissDuplicate();
        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} possible-duplicate flag dismissed by {UserId}",
            report.Id, currentUser.UserId);

        return Result.Success();
    }
}
