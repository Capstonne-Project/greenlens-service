using Greenlens.Domain.Common;
using MediatR;

namespace Greenlens.Application.Features.Analytics.GetAdminQueueAging;

public sealed record GetAdminQueueAgingQuery : IRequest<Result<List<QueueAgingBucket>>>;

public sealed record QueueAgingBucket(
    string Range,
    int Count);
