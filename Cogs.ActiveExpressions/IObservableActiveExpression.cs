namespace Cogs.ActiveExpressions;

/// <summary>
/// Represents an active expression that is observable
/// </summary>
/// <typeparam name="TValue">The type of the active expression's value</typeparam>
public interface IObservableActiveExpression<TValue>
{
    /// <summary>
    /// Adds an observer of this active expression
    /// </summary>
    /// <param name="observer"></param>
    void AddActiveExpressionOserver(IObserveActiveExpressions<TValue> observer);

    /// <summary>
    /// Removes an observer of this active expression
    /// </summary>
    /// <param name="observer"></param>
    void RemoveActiveExpressionObserver(IObserveActiveExpressions<TValue> observer);
}
