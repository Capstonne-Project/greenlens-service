using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ConfirmDuplicate;

/// <summary>LEO confirms a report is a duplicate of a primary report and merges it. BR-REP-032.</summary>
public sealed record ConfirmDuplicateCommand(
    Guid ReportId,
    Guid PrimaryReportId) : IRequest<Result>;
