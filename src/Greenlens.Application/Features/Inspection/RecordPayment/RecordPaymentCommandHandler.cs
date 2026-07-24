using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using Greenlens.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>
/// BR-INS-020: Record in-person penalty payment at ward/commune office.
/// Creates a PenaltyPayment record with evidence and updates InspectionReport status.
/// </summary>
public sealed class RecordPaymentCommandHandler(
    IInspectionReportRepository inspections,
    ITeamMemberRepository teamMembers,
    ICurrentUser currentUser,
    IUnitOfWork uow,
    ILogger<RecordPaymentCommandHandler> logger)
    : IRequestHandler<RecordPaymentCommand, Result>
{
    public async Task<Result> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting record payment");

        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for inspection {InspectionId}", request.InspectionId);
            return Errors.Inspections.InspectionNotFound;
        }

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
        {
            logger.LogWarning("Team leader validation failed for inspection {InspectionId}", request.InspectionId);
            return authError;
        }

        // Create PenaltyPayment record (in-person at ward office)
        var payment = PenaltyPayment.Create(
            inspection.Id,
            request.PaidAmount,
            request.PaidAt,
            currentUser.UserId,
            request.EvidenceUrl,
            request.Note);

        var result = inspection.RecordPayment(payment);
        if (result.IsFailure)
        {
            logger.LogWarning("Failed to record payment for inspection {InspectionId}", request.InspectionId);
            return result;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Payment {Amount} VND recorded on InspectionReport {Id} (paid at {PaidAt}). New status: {Status}, total paid: {TotalPaid}/{Total}",
            request.PaidAmount, inspection.Id, request.PaidAt, inspection.Status,
            inspection.PaidAmount, inspection.PenaltyAmount);

        return Result.Success();
    }
}
