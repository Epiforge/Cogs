namespace Cogs.Expressions;

/// <summary>
/// An exception was thrown while processing an exception
/// </summary>
public class ExpressionProcessingException :
    Exception
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="ExpressionProcessingException"/>
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="innerException"></param>
    public ExpressionProcessingException(Expression expression, Exception innerException) :
        base("An unexpected exception occurred while processing an expression", innerException) =>
        Expression = expression;

    /// <summary>
    /// Gets the expression that was being processed when the exception was thrown
    /// </summary>
    public Expression Expression { get; }
}
