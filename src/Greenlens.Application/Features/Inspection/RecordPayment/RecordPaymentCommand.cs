using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.RecordPayment;

/// <summary>BR-INS-020: Record penalty payment (full or partial).</summary>
public sealed record RecordPaymentCommand(
    Guid InspectionId,
    decimal PaidAmount) : IRequest<Result>;
