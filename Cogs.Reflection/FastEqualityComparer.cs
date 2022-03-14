namespace Cogs.Reflection;

/// <summary>
/// Provides methods for testing equality of and getting hash codes for instances of a type that is not known at compile time
/// </summary>
public class FastEqualityComparer
{
    FastEqualityComparer(Type type)
    {
        Type = type;
        var equalityComparerType = typeof(EqualityComparer<>).MakeGenericType(type);
        equalityComparer = equalityComparerType.GetRuntimeProperty(nameof(EqualityComparer<object>.Default)).GetValue(null);
        equals = FastMethodInfo.Get(equalityComparerType.GetRuntimeMethod(nameof(EqualityComparer<object>.Equals), new Type[] { type, type }));
        getHashCode = FastMethodInfo.Get(equalityComparerType.GetRuntimeMethod(nameof(EqualityComparer<object>.GetHashCode), new Type[] { type }));
    }

    readonly object equalityComparer;
    readonly FastMethodInfo equals;
    readonly FastMethodInfo getHashCode;

    /// <summary>
    /// Determines whether the specified objects of the type indicated by <see cref="Type"/> are equal
    /// </summary>
    /// <param name="x">The first object to compare</param>
    /// <param name="y">The second object to compare</param>
    /// <returns><c>true</c> if the specified objects are equal; otherwise, <c>false</c></returns>
    public new bool Equals(object? x, object? y) => equals.Invoke(equalityComparer, x, y) is bool equality ? equality : throw new Exception("Equality test failed");

    /// <summary>
    /// Returns a hash code for the specified object of the type indicated by <see cref="Type"/>
    /// </summary>
    /// <param name="obj">The obect for which a hash code is to be returned</param>
    /// <returns>A hash code for the specified object</returns>
    public int GetHashCode(object obj) => getHashCode.Invoke(equalityComparer, obj) is int hashCode ? hashCode : throw new Exception("Getting hash code failed");

    /// <summary>
    /// Gets the type for which this <see cref="FastEqualityComparer"/> tests equality and gets hash codes
    /// </summary>
    public Type Type { get; }

    static readonly ConcurrentDictionary<Type, FastEqualityComparer> equalityComparers = new();

    /// <summary>
    /// Gets a <see cref="FastEqualityComparer"/> for the specified type
    /// </summary>
    /// <param name="type">The type</param>
    /// <returns>A <see cref="FastEqualityComparer"/></returns>
    public static FastEqualityComparer Get(Type type) =>
        type is null ? throw new ArgumentNullException(nameof(type)) : equalityComparers.GetOrAdd(type, Factory);

    static FastEqualityComparer Factory(Type type) => new(type);
}
