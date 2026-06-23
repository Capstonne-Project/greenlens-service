using Greenlens.Application.Common;
using Greenlens.Application.Common.Interfaces.Persistence;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Inspection.CloseInspection;

/// <summary>Close inspection after full payment (Paid → Closed).</summary>
public sealed class CloseInspectionCommandHandler(
    IInspectionReportRepository inspections,
    IUnitOfWork uow,
    ILogger<CloseInspectionCommandHandler> logger)
    : IRequestHandler<CloseInspectionCommand, Result>
{
    public async Task<Result> Handle(CloseInspectionCommand request, CancellationToken ct)
    {
        var inspection = await inspections.GetByIdAsync(request.InspectionId, ct).ConfigureAwait(false);
        if (inspection is null)
            return Errors.Inspections.InspectionNotFound;

        var result = inspection.Close(request.Reason);
        if (result.IsFailure) return result;

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("InspectionReport {Id} CLOSED", request.InspectionId);
        return Result.Success();
    }
}
