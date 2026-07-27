using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Reports.ApproveReopenRequest;

public sealed record ApproveReopenRequestCommand(
    Guid ReportId,
    Guid RequestId) : IRequest<Result>;
