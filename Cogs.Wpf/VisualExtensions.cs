namespace Cogs.Wpf;

/// <summary>
/// Provides extension methods for visuals
/// </summary>
public static class VisualExtensions
{
    /// <summary>
    /// Gets the first ancestor of <paramref name="reference"/> in the Visual Tree of type <typeparamref name="T"/>, or <c>null</c> if none could be found
    /// </summary>
    /// <typeparam name="T">The type of ancestor to find</typeparam>
    /// <param name="reference">The reference in the Visual Tree from which to begin searching</param>
    public static T? GetVisualAncestor<T>(this DependencyObject reference) where T : DependencyObject
    {
        do
            reference = VisualTreeHelper.GetParent(reference);
        while (reference is not null and not T);
        return reference is T typedAncestor ? typedAncestor : null;
    }

    /// <summary>
    /// Gets the first ancestor of <paramref name="reference"/> in the Visual Tree of type <typeparamref name="T1"/> or type <typeparamref name="T2"/>, or <c>null</c> if none could be found
    /// </summary>
    /// <typeparam name="T1">The first type of ancestor to find</typeparam>
    /// <typeparam name="T2">The second type of ancestor to find</typeparam>
    /// <param name="reference">The reference in the Visual Tree from which to begin searching</param>
    public static DependencyObject? GetVisualAncestor<T1, T2>(this DependencyObject reference) where T1 : DependencyObject where T2 : DependencyObject
    {
        do
            reference = VisualTreeHelper.GetParent(reference);
        while (reference is not null and not T1 and not T2);
        return reference;
    }

    /// <summary>
    /// Gets the first ancestor of <paramref name="reference"/> in the Visual Tree of type <typeparamref name="T1"/>, type <typeparamref name="T2"/>, or type <typeparamref name="T3"/>, or <c>null</c> if none could be found
    /// </summary>
    /// <typeparam name="T1">The first type of ancestor to find</typeparam>
    /// <typeparam name="T2">The second type of ancestor to find</typeparam>
    /// <typeparam name="T3">The third type of ancestor to find</typeparam>
    /// <param name="reference">The reference in the Visual Tree from which to begin searching</param>
    public static DependencyObject? GetVisualAncestor<T1, T2, T3>(this DependencyObject reference) where T1 : DependencyObject where T2 : DependencyObject where T3 : DependencyObject
    {
        do
            reference = VisualTreeHelper.GetParent(reference);
        while (reference is not null and not T1 and not T2 and not T3);
        return reference;
    }

    /// <summary>
    /// Gets the first member of a Visual Tree descending from a <paramref name="reference"/> of type <typeparamref name="T"/>, or <c>null</c> if none could be found
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reference">The reference in the Visual Tree from which to begin searching</param>
    public static T? GetVisualDescendent<T>(this DependencyObject reference) where T : DependencyObject
    {
        if (reference is T typedReference)
            return typedReference;
        for (int i = 0, ii = VisualTreeHelper.GetChildrenCount(reference); i < ii; ++i)
            if (GetVisualDescendent<T>(VisualTreeHelper.GetChild(reference, i)) is { } foundDescendent)
                return foundDescendent;
        return null;
    }

    /// <summary>
    /// Gets the first member of a Visual Tree descending from a <paramref name="reference"/> of type <typeparamref name="T"/> that satisfies <paramref name="predicate"/>, or <c>null</c> if none could be found
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reference">The reference in the Visual Tree from which to begin searching</param>
    /// <param name="predicate">The predicate test which must be satisified</param>
    public static T? GetVisualDescendent<T>(this DependencyObject reference, Func<T, bool> predicate) where T : DependencyObject
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));
        if (reference is T typedReference && predicate(typedReference))
            return typedReference;
        for (int i = 0, ii = VisualTreeHelper.GetChildrenCount(reference); i < ii; ++i)
            if (GetVisualDescendent(VisualTreeHelper.GetChild(reference, i), predicate) is { } foundDescendent)
                return foundDescendent;
        return null;
    }
}
