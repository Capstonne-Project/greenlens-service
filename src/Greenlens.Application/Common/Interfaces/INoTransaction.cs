namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Marker for MediatR commands that must not be wrapped in TransactionBehavior
/// (typically side-effect handlers invoked from deferred domain events).
/// </summary>
public interface INoTransaction;
