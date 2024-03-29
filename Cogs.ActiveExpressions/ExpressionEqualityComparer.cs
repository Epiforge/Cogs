namespace Cogs.ActiveExpressions;

/// <summary>
/// Defines methods to support the comparison of <see cref="Expression"/> objects for equality
/// </summary>
public sealed class ExpressionEqualityComparer :
    IEqualityComparer<Expression>
{
    static ExpressionEqualityComparer() =>
        Default = new ExpressionEqualityComparer();

    /// <summary>
    /// Gets the default instance of <see cref="ExpressionEqualityComparer"/>
    /// </summary>
    public static ExpressionEqualityComparer Default { get; }

    /// <summary>
    /// Determines whether the specified <see cref="Expression"/> objects are equal
    /// </summary>
    /// <param name="x">The first <see cref="Expression"/> to compare</param>
    /// <param name="y">The second <see cref="Expression"/> to compare</param>
    /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c></returns>
    public bool Equals(Expression x, Expression y) =>
        new ExpressionDiagramVisitor(x).Elements.SequenceEqual(new ExpressionDiagramVisitor(y).Elements);

    /// <summary>
    /// Returns a hash code for the specified <see cref="Expression"/>
    /// </summary>
    /// <param name="obj">The <see cref="Expression"/> for which a hash code is to be returned</param>
    /// <returns>A hash code for the specified <see cref="Expression"/></returns>
    public int GetHashCode(Expression obj)
    {
        var elements = new ExpressionDiagramVisitor(obj).Elements;
        var hashCode = elements[0]?.GetHashCode() ?? 0;
        for (int i = 1, ii = elements.Count; i < ii; ++i)
            hashCode = HashCode.Combine(hashCode, elements[i]);
        return hashCode;
    }
}
