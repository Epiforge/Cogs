namespace Cogs.ActiveExpressions;

/// <summary>
/// Represents an active evaluation of a lambda expression
/// </summary>
/// <typeparam name="TResult">The type of the value returned by the lambda expression upon which this active expression is based</typeparam>
public interface IActiveExpression<out TResult> :
    IDisposable,
    IDisposalStatus,
    INotifyDisposalOverridden,
    INotifyDisposed,
    INotifyDisposing,
    INotifyPropertyChanged,
    INotifyPropertyChanging
{
    /// <summary>
    /// Gets the arguments that were passed to the lambda expression
    /// </summary>
    IReadOnlyList<object?> Arguments { get; }

    /// <summary>
    /// Gets the exception that was thrown while evaluating the lambda expression; <c>null</c> if there was no such exception
    /// </summary>
    Exception? Fault { get; }

    /// <summary>
    /// Gets the options used when creating the active expression
    /// </summary>
    ActiveExpressionOptions? Options { get; }

    /// <summary>
    /// Gets the result of evaluating the lambda expression
    /// </summary>
    TResult? Value { get; }
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with a single argument
/// </summary>
/// <typeparam name="TArg">The type of the argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
public interface IActiveExpression<out TArg, out TResult> :
    IActiveExpression<TResult>,
    IDisposable,
    IDisposalStatus,
    INotifyDisposalOverridden,
    INotifyDisposed,
    INotifyDisposing,
    INotifyPropertyChanged,
    INotifyPropertyChanging
{
    /// <summary>
    /// Gets the argument that was passed to the lambda expression
    /// </summary>
    TArg Arg { get; }
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with two arguments
/// </summary>
/// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
[SuppressMessage("Code Analysis", "CA1005: Avoid excessive parameters on generic types")]
public interface IActiveExpression<out TArg1, out TArg2, out TResult> :
    IActiveExpression<TResult>,
    IDisposable,
    IDisposalStatus,
    INotifyDisposalOverridden,
    INotifyDisposed,
    INotifyDisposing,
    INotifyPropertyChanged,
    INotifyPropertyChanging
{
    /// <summary>
    /// Gets the first argument that was passed to the lambda expression
    /// </summary>
    TArg1 Arg1 { get; }

    /// <summary>
    /// Gets the second argument that was passed to the lambda expression
    /// </summary>
    TArg2 Arg2 { get; }
}

/// <summary>
/// Represents an active evaluation of a strongly-typed lambda expression with three arguments
/// </summary>
/// <typeparam name="TArg1">The type of the first argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg2">The type of the second argument passed to the lambda expression</typeparam>
/// <typeparam name="TArg3">The type of the third argument passed to the lambda expression</typeparam>
/// <typeparam name="TResult">The type of the value returned by the expression upon which this active expression is based</typeparam>
[SuppressMessage("Code Analysis", "CA1005: Avoid excessive parameters on generic types")]
public interface IActiveExpression<out TArg1, out TArg2, out TArg3, out TResult> :
    IActiveExpression<TArg1, TArg2, TResult>
{
    /// <summary>
    /// Gets the third argument that was passed to the lambda expression
    /// </summary>
    TArg3 Arg3 { get; }
}
