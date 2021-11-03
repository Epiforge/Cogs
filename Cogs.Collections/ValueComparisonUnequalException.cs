namespace Cogs.Collections;

/// <summary>
/// Represents when the valueFactory used by <see cref="ObservableConcurrentDictionary{TKey, TValue}.TryUpdate(TKey, TValue, TValue)"/> finds the oldValue and comparisonValue are unequal
/// </summary>
public class ValueComparisonUnequalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
    /// </summary>
    public ValueComparisonUnequalException() : base("the oldValue and comparisonValue are unequal")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ValueComparisonUnequalException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
    public ValueComparisonUnequalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
