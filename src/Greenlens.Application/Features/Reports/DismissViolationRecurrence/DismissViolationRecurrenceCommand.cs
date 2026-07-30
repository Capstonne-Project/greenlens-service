using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DismissViolationRecurrence;

/// <summary>LEO clears the violation-recurrence suspicion flag (BR-REP-034).</summary>
public sealed record DismissViolationRecurrenceCommand(Guid ReportId) : IRequest<Result>;
