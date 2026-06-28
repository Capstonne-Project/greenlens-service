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
/// Rollback is automatic on any exception; UnitOfWork.SaveChangesAsync commits
/// entity changes and dispatches domain events inside the transaction boundary.
/// </remarks>
public sealed class TransactionBehavior<TRequest, TResponse>(
    ITransactionManager transactionManager,
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

            return response;
        }
        catch
        {
            await transactionManager.RollbackAsync(cancellationToken).ConfigureAwait(false);

            logger.LogWarning("Transaction rolled back for {Request}", requestName);

            throw;
        }
    }
}
