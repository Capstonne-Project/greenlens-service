using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CloseNoViolation;

/// <summary>BR-INS-013: Close inspection — no violation found.</summary>
public sealed class CloseNoViolationCommandHandler(
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ILogger<CloseNoViolationCommandHandler> logger)
    : IRequestHandler<CloseNoViolationCommand, Result>
{
    public async Task<Result> Handle(CloseNoViolationCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var result = inspection.CloseNoViolation(request.Reason);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("InspectionReport {Id} closed — no violation found", request.InspectionId);
        return Result.Success();
    }
}
