namespace Cogs.Collections;

/// <summary>
/// Provides extensions for dealing with <see cref="IDictionary"/> and <see cref="IDictionary{TKey, TValue}"/>
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Adds a key/value pair to the specified <see cref="IDictionary"/> by using the specified function if the key does not already exist (returns the new value, or the existing value if the key exists)
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary"/></param>
    /// <param name="key">The key of the element to add</param>
    /// <param name="valueFactory">The function used to generate a value for the key</param>
    /// <returns>The value for the key (this will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="valueFactory"/> is <c>null</c></exception>
    /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
    public static object GetOrAdd(this IDictionary dictionary, object key, Func<object, object> valueFactory)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        if (valueFactory is null)
            throw new ArgumentNullException(nameof(valueFactory));
        if (dictionary.Contains(key))
            return dictionary[key];
        var value = valueFactory(key);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    /// Adds a key/value pair to the specified <see cref="IDictionary{TKey, TValue}"/> by using the specified function if the key does not already exist (returns the new value, or the existing value if the key exists)
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/></param>
    /// <param name="key">The key of the element to add</param>
    /// <param name="valueFactory">The function used to generate a value for the key</param>
    /// <returns>The value for the key (this will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="valueFactory"/> is <c>null</c></exception>
    /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        if (valueFactory is null)
            throw new ArgumentNullException(nameof(valueFactory));
        if (dictionary.TryGetValue(key, out var value))
            return value;
        value = valueFactory(key);
        dictionary.Add(key, value);
        return value;
    }

    /// <summary>
    /// Attempts to remove and return the value that has the specified key from the specified <see cref="IDictionary"/>
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary"/></param>
    /// <param name="key">The key of the element to remove and return</param>
    /// <param name="value">When this method returns, contains the object removed from the <see cref="IDictionary"/>, or <c>null</c> if key does not exist</param>
    /// <returns><c>true</c> if the object was removed successfully; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
    public static bool TryRemove(this IDictionary dictionary, object key, [NotNullWhen(true)] out object? value)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (dictionary.Contains(key))
        {
            value = dictionary[key];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Attempts to remove and return the value that has the specified key from the specified <see cref="IDictionary{TKey, TValue}"/>
    /// </summary>
    /// <param name="dictionary">The <see cref="IDictionary{TKey, TValue}"/></param>
    /// <param name="key">The key of the element to remove and return</param>
    /// <param name="value">When this method returns, contains the object removed from the <see cref="IDictionary{TKey, TValue}"/>, or the default value of the <typeparamref name="TValue"/> type if key does not exist</param>
    /// <returns><c>true</c> if the object was removed successfully; otherwise, <c>false</c></returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
    public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
    {
        if (dictionary is null)
            throw new ArgumentNullException(nameof(dictionary));
        if (key is null)
            throw new ArgumentNullException(nameof(key));
        if (dictionary.TryGetValue(key, out value))
        {
            dictionary.Remove(key);
            return true;
        }
        return false;
    }
}
