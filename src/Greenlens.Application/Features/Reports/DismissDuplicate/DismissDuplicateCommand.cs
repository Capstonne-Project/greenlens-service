using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.DismissDuplicate;

/// <summary>LEO dismisses a possible-duplicate flag (not actually a duplicate). BR-REP-031.</summary>
public sealed record DismissDuplicateCommand(Guid ReportId) : IRequest<Result>;
