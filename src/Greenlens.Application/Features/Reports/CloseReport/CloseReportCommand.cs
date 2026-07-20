using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.CloseReport;

/// <summary>Citizen/auto close. BR-REP-016.</summary>
public sealed record CloseReportCommand(Guid ReportId) : IRequest<Result>;
