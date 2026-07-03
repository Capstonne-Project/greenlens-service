using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>BR-INS-020: Record payment. Auto-determines Paid vs PartiallyPaid.</summary>
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
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var authError = await InspectionTeamAuthorization.ValidateTeamLeaderAsync(
            inspection, teamMembers, currentUser, ct).ConfigureAwait(false);
        if (authError is not null)
            return authError;

        var result = inspection.RecordPayment(request.PaidAmount);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Payment {Amount} recorded on InspectionReport {Id}. New status: {Status}",
            request.PaidAmount, inspection.Id, inspection.Status);

        return Result.Success();
    }
}
