namespace Cogs.Reflection;

/// <summary>
/// Provides extensions for reflection
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Searches for the events of the current <see cref="Type"/>, including interfaces and interface inheritance, using the specified binding constraints
    /// </summary>
    /// <param name="type">The <see cref="Type"/></param>
    /// <param name="bindingAttr">A bitwise combination of the enumeration values that specify how the search is conducted</param>
    public static EventInfo[] GetImplementationEvents(this Type type, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (type.IsInterface)
        {
            var eventInfos = new List<EventInfo>();
            var considered = new List<Type>();
            var queue = new Queue<Type>();
            considered.Add(type);
            queue.Enqueue(type);
            while (queue.Count > 0)
            {
                var subType = queue.Dequeue();
                foreach (var subInterface in subType.GetInterfaces())
                {
                    if (considered.Contains(subInterface))
                        continue;
                    considered.Add(subInterface);
                    queue.Enqueue(subInterface);
                }
                eventInfos.InsertRange(0, subType.GetEvents(bindingAttr).Where(x => !eventInfos.Contains(x)));
            }
            return eventInfos.ToArray();
        }
        return type.GetEvents(bindingAttr);
    }

    /// <summary>
    /// Searches for the methods of the current <see cref="Type"/>, including interfaces and interface inheritance, using the specified binding constraints
    /// </summary>
    /// <param name="type">The <see cref="Type"/></param>
    /// <param name="bindingAttr">A bitwise combination of the enumeration values that specify how the search is conducted</param>
    public static MethodInfo[] GetImplementationMethods(this Type type, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (type.IsInterface)
        {
            var methodInfos = new List<MethodInfo>();
            var considered = new List<Type>();
            var queue = new Queue<Type>();
            considered.Add(type);
            queue.Enqueue(type);
            while (queue.Count > 0)
            {
                var subType = queue.Dequeue();
                foreach (var subInterface in subType.GetInterfaces())
                {
                    if (considered.Contains(subInterface))
                        continue;
                    considered.Add(subInterface);
                    queue.Enqueue(subInterface);
                }
                methodInfos.InsertRange(0, subType.GetMethods(bindingAttr).Where(x => !methodInfos.Contains(x)));
            }
            return methodInfos.ToArray();
        }
        return type.GetMethods(bindingAttr);
    }

    /// <summary>
    /// Searches for the properties of the current <see cref="Type"/>, including interfaces and interface inheritance, using the specified binding constraints
    /// </summary>
    /// <param name="type">The <see cref="Type"/></param>
    /// <param name="bindingAttr">A bitwise combination of the enumeration values that specify how the search is conducted</param>
    public static PropertyInfo[] GetImplementationProperties(this Type type, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (type.IsInterface)
        {
            var propertyInfos = new List<PropertyInfo>();
            var considered = new List<Type>();
            var queue = new Queue<Type>();
            considered.Add(type);
            queue.Enqueue(type);
            while (queue.Count > 0)
            {
                var subType = queue.Dequeue();
                foreach (var subInterface in subType.GetInterfaces())
                {
                    if (considered.Contains(subInterface))
                        continue;
                    considered.Add(subInterface);
                    queue.Enqueue(subInterface);
                }
                propertyInfos.InsertRange(0, subType.GetProperties(bindingAttr).Where(x => !propertyInfos.Contains(x)));
            }
            return propertyInfos.ToArray();
        }
        return type.GetProperties(bindingAttr);
    }
}
