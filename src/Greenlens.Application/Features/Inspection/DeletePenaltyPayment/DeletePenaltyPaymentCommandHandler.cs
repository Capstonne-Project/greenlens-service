using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.DeletePenaltyPayment;

public sealed class DeletePenaltyPaymentCommandHandler(
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    ILogger<DeletePenaltyPaymentCommandHandler> logger) : IRequestHandler<DeletePenaltyPaymentCommand, Result>
{
    public async Task<Result> Handle(DeletePenaltyPaymentCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByPaymentIdAsync(request.PaymentId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.PaymentNotFound; // Not found payment because inspection not found

        var payment = inspection.Payments.FirstOrDefault(p => p.Id == request.PaymentId);
        if (payment is null)
            return Errors.Inspections.PaymentNotFound;

        payment.SoftDelete(currentUser.UserId.ToString());
        
        var removeResult = inspection.RemovePayment(payment);
        if (removeResult.IsFailure)
            return removeResult;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation("PenaltyPayment {PaymentId} soft-deleted by {UserId}. Inspection {InspectionId} PaidAmount updated.", 
            request.PaymentId, currentUser.UserId, inspection.Id);

        return Result.Success();
    }
}
