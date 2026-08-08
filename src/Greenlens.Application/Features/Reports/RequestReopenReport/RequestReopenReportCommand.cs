using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.RequestReopenReport;

/// <summary>Citizen submits reopen request with reason and evidence (BR-REP-015).</summary>
public sealed record RequestReopenReportCommand(
    Guid ReportId,
    string Reason,
    IReadOnlyList<string> ImageUrls,
    string? VideoUrl = null) : IRequest<Result<Guid>>;
