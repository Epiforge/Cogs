using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Cogs.Reflection
{
    /// <summary>
    /// Provides a method for getting the default value of a type that is not known at compile time
    /// </summary>
    public static class FastDefault
    {
        static readonly ConcurrentDictionary<Type, object?> defaults = new ConcurrentDictionary<Type, object?>();
        static readonly MethodInfo getDefaultValueMethod = typeof(FastDefault).GetRuntimeMethods().Single(method => method.Name == nameof(GetDefaultValue));

        static object? CreateDefault(Type type) => getDefaultValueMethod.MakeGenericMethod(type).Invoke(null, null);

        [return: MaybeNull]
        static T GetDefaultValue<T>() => default;

        /// <summary>
        /// Gets the default value for the specified type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The default value</returns>
        public static object? Get(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            return defaults.GetOrAdd(type, CreateDefault);
        }
    }
}
