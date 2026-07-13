using Greenlens.Domain.Common;
using Greenlens.Domain.Enums;
using MediatR;

namespace Greenlens.Application.Features.Reports.FlagReport;

/// <summary>Citizen flags a report (duplicate/invalid/spam/inappropriate). BR-REP-033.</summary>
public sealed record FlagReportCommand(
    Guid ReportId,
    FlagType Type,
    string? Reason) : IRequest<Result>;
