using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.DeletePenaltyPayment;

/// <summary>
/// Soft-delete a PenaltyPayment.
/// Adjusts the remaining PaidAmount in the InspectionReport.
/// Only Admin, LEO, Inspector can perform this.
/// </summary>
public sealed record DeletePenaltyPaymentCommand(Guid PaymentId) : IRequest<Result>;
