using System.Text.Json;
using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.DeletePenaltyPayment;

/// <summary>Soft-delete a penalty payment record and recalculate inspection paid amount.</summary>
/// <remarks>Implements: BR-INS-020 (payment correction), BR-ADM-010.</remarks>
public sealed class DeletePenaltyPaymentCommandHandler(
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ICurrentUser currentUser,
    IAuditLogger auditLogger,
    ILogger<DeletePenaltyPaymentCommandHandler> logger) : IRequestHandler<DeletePenaltyPaymentCommand, Result>
{
    public async Task<Result> Handle(DeletePenaltyPaymentCommand request, CancellationToken ct)
    {
        logger.LogInformation("Getting delete penalty payment");

        var existingPayment = await inspections.FindPaymentByIdAsync(request.PaymentId, ct).ConfigureAwait(false);
        if (existingPayment is null)
        {
            logger.LogWarning("Payment not found for payment {PaymentId}", request.PaymentId);
            return Errors.Inspections.PaymentNotFound;
        }

        if (existingPayment.IsDeleted)
        {
            logger.LogWarning("Payment {PaymentId} already deleted", request.PaymentId);
            return Errors.Inspections.PaymentAlreadyDeleted;
        }

        var inspection = await inspections.GetByPaymentIdAsync(request.PaymentId, ct).ConfigureAwait(false);
        if (inspection is null)
        {
            logger.LogWarning("Inspection not found for payment {PaymentId}", request.PaymentId);
            return Errors.Inspections.PaymentNotFound;
        }

        var payment = inspection.Payments.FirstOrDefault(p => p.Id == request.PaymentId);
        if (payment is null)
        {
            logger.LogWarning("Payment not found for payment {PaymentId}", request.PaymentId);
            return Errors.Inspections.PaymentNotFound;
        }

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            paidAmount = inspection.PaidAmount,
            paymentAmount = payment.Amount
        });

        payment.SoftDelete(currentUser.UserId.ToString());

        var removeResult = inspection.RemovePayment(payment);
        if (removeResult.IsFailure)
        {
            logger.LogWarning("Failed to remove payment {PaymentId} from inspection {InspectionId}", request.PaymentId, inspection.Id);
            return removeResult;
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        await auditLogger.LogAsync(
            "DeletePenaltyPayment",
            "PenaltyPayment",
            request.PaymentId.ToString(),
            oldValues: oldSnapshot,
            newValues: JsonSerializer.Serialize(new
            {
                isDeleted = true,
                inspectionId = inspection.Id,
                paidAmount = inspection.PaidAmount
            }),
            ct).ConfigureAwait(false);

        logger.LogInformation(
            "PenaltyPayment {PaymentId} soft-deleted by {UserId}. Inspection {InspectionId} PaidAmount updated.",
            request.PaymentId, currentUser.UserId, inspection.Id);

        return Result.Success();
    }
}
