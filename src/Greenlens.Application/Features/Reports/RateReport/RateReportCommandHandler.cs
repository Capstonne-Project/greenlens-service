using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using Greenlens.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.RateReport;

/// <summary>
/// Citizen đánh giá sau khi báo cáo được giải quyết (Resolved/Closed).
/// </summary>
/// <remarks>
/// Implements: BR-REP-018 — citizen feedback after resolution.
/// One rating per report per user.
/// </remarks>
public sealed class RateReportCommandHandler(
    IReportRepository reports,
    IGenericRepository<ReportSatisfaction> satisfactions,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RateReportCommandHandler> logger)
    : IRequestHandler<RateReportCommand, Result<RateReportResponse>>
{
    public async Task<Result<RateReportResponse>> Handle(
        RateReportCommand request,
        CancellationToken cancellationToken)
    {
        var report = await reports.GetByIdAsync(request.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (report is null)
            return Errors.Reports.ReportNotFound;

        // Only Resolved or Closed reports can be rated
        if (report.Status is not (ReportStatus.Resolved or ReportStatus.Closed))
            return Errors.Reports.InvalidStatusTransition;

        // Only the reporter can rate
        if (report.ReporterId != currentUser.UserId)
            return Errors.Reports.NotReporter;

        // Check if already rated (one per report per user)
        var alreadyRated = await satisfactions.ExistsAsync(
            s => s.ReportId == request.ReportId && s.UserId == currentUser.UserId,
            cancellationToken).ConfigureAwait(false);

        if (alreadyRated)
            return Errors.Reports.AlreadyRated;

        var satisfaction = ReportSatisfaction.Create(
            request.ReportId,
            currentUser.UserId,
            request.IsSatisfied,
            request.Rating,
            request.Comment);

        satisfactions.Add(satisfaction);
        await uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Report {ReportId} rated by {UserId}: satisfied={IsSatisfied}, rating={Rating}",
            request.ReportId, currentUser.UserId, request.IsSatisfied, request.Rating);

        return new RateReportResponse(satisfaction.Id);
    }
}
