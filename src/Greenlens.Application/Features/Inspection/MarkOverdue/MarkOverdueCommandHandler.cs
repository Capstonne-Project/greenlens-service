using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.MarkOverdue;

/// <summary>BR-INS-021: Mark inspection as overdue. Called by system/background job.</summary>
public sealed class MarkOverdueCommandHandler(
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ILogger<MarkOverdueCommandHandler> logger)
    : IRequestHandler<MarkOverdueCommand, Result>
{
    public async Task<Result> Handle(MarkOverdueCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var result = inspection.MarkOverdue();
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogWarning("InspectionReport {Id} marked as OVERDUE", request.InspectionId);
        return Result.Success();
    }
}
