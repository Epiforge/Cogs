namespace Cogs.ActiveExpressions;

/// <summary>
/// Represents an observer of active expressions
/// </summary>
/// <typeparam name="TValue"></typeparam>
public interface IObserveActiveExpressions<in TValue>
{
    /// <summary>
    /// An observed active expression has changed
    /// </summary>
    /// <param name="activeExpression">The active expression that has changed</param>
    /// <param name="oldValue">The previous value</param>
    /// <param name="newValue">The new value</param>
    /// <param name="oldFault">The previous fault</param>
    /// <param name="newFault">The new fault</param>
    void ActiveExpressionChanged(IObservableActiveExpression<TValue> activeExpression, TValue? oldValue, TValue? newValue, Exception? oldFault, Exception? newFault);
}
