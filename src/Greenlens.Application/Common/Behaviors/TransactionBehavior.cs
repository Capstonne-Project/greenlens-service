using Greenlens.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Greenlens.Application.Common.Behaviors;

/// <summary>
/// Wraps Command handlers in a database transaction.
/// Skips Query handlers (by naming convention: request type name ends with "Command").
/// </summary>
/// <remarks>
/// Pipeline order: Validation → Transaction → Logging → Handler.
/// Rollback is automatic on any exception. Domain events are deferred until
/// after the transaction commits to prevent re-entrant SaveChanges failures.
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse>(
    ITransactionManager transactionManager,
    IDomainEventCollector eventCollector,
    IPublisher publisher,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly bool IsCommand =
        typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand)
            return await next().ConfigureAwait(false);

        var requestName = typeof(TRequest).Name;

        await transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await next().ConfigureAwait(false);

            await transactionManager.CommitAsync(cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Transaction committed for {Request}", requestName);

            await PublishDeferredDomainEventsAsync(cancellationToken).ConfigureAwait(false);

            return response;
        }
        catch
        {
            eventCollector.Clear();

            await transactionManager.RollbackAsync(cancellationToken).ConfigureAwait(false);

            logger.LogWarning("Transaction rolled back for {Request}", requestName);

            throw;
        }
    }

    private async Task PublishDeferredDomainEventsAsync(CancellationToken ct)
    {
        var deferred = eventCollector.DrainAll();
        if (deferred.Count == 0)
            return;

        foreach (var domainEvent in deferred)
        {
            await publisher.Publish(domainEvent, ct).ConfigureAwait(false);
        }
    }
}
