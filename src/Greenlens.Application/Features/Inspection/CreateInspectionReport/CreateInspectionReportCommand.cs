using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Inspection.CreateInspectionReport;

/// <summary>
/// BR-INS-001, BR-OFF-005: LEO raises an InspectionReport linked to a verified Report.
/// Optionally assigns an Inspection Team immediately.
/// </summary>
public sealed record CreateInspectionReportCommand(
    Guid ReportId,
    Guid? AssignedTeamId,
    string? ViolationDescription,
    string? ViolatorName,
    string? ViolatorAddress,
    string? ViolatorIdentity) : IRequest<Result<Guid>>;
