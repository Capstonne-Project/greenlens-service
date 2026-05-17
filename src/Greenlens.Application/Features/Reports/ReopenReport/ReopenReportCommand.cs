using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ReopenReport;

/// <summary>Citizen reopens resolved report. BR-REP-015.</summary>
public sealed record ReopenReportCommand(Guid ReportId) : IRequest<Result>;
