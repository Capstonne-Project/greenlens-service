using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DeleteReport;

/// <summary>BR-REP-017: Citizen soft-deletes a report (only Submitted, no AI/Officer interaction).</summary>
public sealed record DeleteReportCommand(Guid ReportId) : IRequest<Result>;
