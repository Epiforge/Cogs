namespace Cogs.Collections
{
    /// <summary>
    /// Represents the method that handles the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection</typeparam>
    /// <param name="sender">The object that raised the event</param>
    /// <param name="e">Information about the event</param>
    public delegate void NotifyGenericCollectionChangedEventHandler<in T>(object sender, INotifyGenericCollectionChangedEventArgs<T> e);
}
