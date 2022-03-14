namespace Cogs.Reflection;

/// <summary>
/// Provides a method for comparing instances of a type that is not known at compile time
/// </summary>
public class FastComparer
{
    FastComparer(Type type)
    {
        Type = type;
        var comparerType = typeof(Comparer<>).MakeGenericType(type);
        comparer = comparerType.GetRuntimeProperty(nameof(Comparer<object>.Default)).GetValue(null);
        compare = FastMethodInfo.Get(comparerType.GetRuntimeMethod(nameof(Comparer<object>.Compare), new Type[] { type }));
    }

    readonly FastMethodInfo compare;
    readonly object comparer;

    /// <summary>
    /// Gets the type for which this <see cref="FastComparer"/> makes comparisons
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Performs a case-sensitive comparison of two objects of the type indicated by <see cref="Type"/> and returns a value indicating whether one is less than, equal to, or greater than the other
    /// </summary>
    /// <param name="x">The first object to compare</param>
    /// <param name="y">The second object to compare</param>
    /// <returns>
    /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table:
    /// <table>
    ///     <tr>
    ///         <th>Value</th>
    ///         <th>Meaning</th>
    ///     </tr>
    ///     <tr>
    ///         <td>Less than zero</td>
    ///         <td><paramref name="x"/> is less than <paramref name="y"/></td>
    ///     </tr>
    ///     <tr>
    ///         <td>Zero</td>
    ///         <td><paramref name="x"/> equals <paramref name="y"/></td>
    ///     </tr>
    ///     <tr>
    ///         <td>Less than zero</td>
    ///         <td><paramref name="x"/> is greater than <paramref name="y"/></td>
    ///     </tr>
    /// </table>
    /// </returns>
    public int Compare(object? x, object? y) => compare.Invoke(comparer, x, y) is int comparison ? comparison : throw new Exception("Comparison failed");

    static readonly ConcurrentDictionary<Type, FastComparer> comparers = new();

    /// <summary>
    /// Gets a <see cref="FastComparer"/> for the specified type
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>A <see cref="FastComparer"/></returns>
    public static FastComparer Get(Type type) =>
        type is null ? throw new ArgumentNullException(nameof(type)) : comparers.GetOrAdd(type, Factory);

    static FastComparer Factory(Type type) => new(type);
}
