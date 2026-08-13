using Greenlens.Application.Features.Analytics.GetAdminQueueAging;
using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetDeoQueueAging;

public sealed record GetDeoQueueAgingQuery : IRequest<Result<List<QueueAgingBucket>>>;
