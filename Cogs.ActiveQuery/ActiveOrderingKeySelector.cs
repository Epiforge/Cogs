namespace Cogs.ActiveQuery;

/// <summary>
/// Represents a selector used by active ordering
/// </summary>
/// <typeparam name="T">The type of elements being ordered</typeparam>
public class ActiveOrderingKeySelector<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveOrderingKeySelector{T}"/> class with the specified key extraction expression for sorting in ascending order
    /// </summary>
    /// <param name="expression">An expression to extract a key from an element</param>
    public ActiveOrderingKeySelector(Expression<Func<T, IComparable>> expression) :
        this(expression, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveOrderingKeySelector{T}"/> class with the specified key extraction expression for sorting in the specified order
    /// </summary>
    /// <param name="expression">An expression to extract a key from am element</param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    public ActiveOrderingKeySelector(Expression<Func<T, IComparable>> expression, bool isDescending) :
        this(expression, null, isDescending)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveOrderingKeySelector{T}"/> class with the specified key extraction expression for sorting in ascending order
    /// </summary>
    /// <param name="expression">An expression to extract a key from an element</param>
    /// <param name="expressionOptions">Options governing the behavior of active expressions created using <paramref name="expression"/></param>
    public ActiveOrderingKeySelector(Expression<Func<T, IComparable>> expression, ActiveExpressionOptions? expressionOptions) :
        this(expression, expressionOptions, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveOrderingKeySelector{T}"/> class with the specified key extraction expression for sorting in the specified order
    /// </summary>
    /// <param name="expression">An expression to extract a key from an element</param>
    /// <param name="expressionOptions">Options governing the behavior of active expressions created using <paramref name="expression"/></param>
    /// <param name="isDescending"><c>true</c> to sort in descending order; otherwise, sort in ascending order</param>
    public ActiveOrderingKeySelector(Expression<Func<T, IComparable>> expression, ActiveExpressionOptions? expressionOptions, bool isDescending)
    {
        Expression = expression;
        ExpressionOptions = expressionOptions;
        IsDescending = isDescending;
    }

    /// <summary>
    /// Gets the expression to extract a key from an element
    /// </summary>
    public Expression<Func<T, IComparable>> Expression { get; }

    /// <summary>
    /// Gets options governing the behavior of active expressions created using <see cref="Expression"/>
    /// </summary>
    public ActiveExpressionOptions? ExpressionOptions { get; }

    /// <summary>
    /// Gets whether the sort is in descending order
    /// </summary>
    public bool IsDescending { get; }
}
