using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.CloseNoViolation;

/// <summary>Inspection Team closes with no violation found. BR-INS-013.</summary>
public sealed record CloseNoViolationCommand(
    Guid ReportId,
    string Reason) : IRequest<Result>;
