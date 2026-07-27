using Greenlens.Application.Common;
using Greenlens.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Features.Reports.ReopenReport;

/// <summary>
/// Deprecated direct reopen. Use POST /reports/{id}/reopen-requests (BR-REP-015 v1.2).
/// </summary>
public sealed class ReopenReportCommandHandler(
    ILogger<ReopenReportCommandHandler> logger) : IRequestHandler<ReopenReportCommand, Result>
{
    public Task<Result> Handle(ReopenReportCommand request, CancellationToken ct)
    {
        logger.LogWarning("Deprecated PUT reopen called for report {ReportId}", request.ReportId);
        return Task.FromResult<Result>(Errors.Reports.ReopenUseRequestEndpoint);
    }
}
