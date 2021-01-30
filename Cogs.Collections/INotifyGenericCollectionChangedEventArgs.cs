using System.Collections.Generic;
using System.Collections.Specialized;

namespace Cogs.Collections
{
    /// <summary>
    /// Provides data for the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    public interface INotifyGenericCollectionChangedEventArgs<out T>
    {
        /// <summary>
        /// Gets the action that caused the event
        /// </summary>
        NotifyCollectionChangedAction Action { get; }

        /// <summary>
        /// Gets the list of new items involved in the change
        /// </summary>
        IReadOnlyList<T> NewItems { get; }

        /// <summary>
        /// Gets the index at which the change occurred
        /// </summary>
        int NewStartingIndex { get; }

        /// <summary>
        /// Gets the list of items affected by a <see cref="NotifyCollectionChangedAction.Replace"/>, <see cref="NotifyCollectionChangedAction.Remove"/>, or <see cref="NotifyCollectionChangedAction.Move"/> action
        /// </summary>
        IReadOnlyList<T> OldItems { get; }

        /// <summary>
        /// Gets the index at which a <see cref="NotifyCollectionChangedAction.Move"/>, <see cref="NotifyCollectionChangedAction.Remove"/>, or <see cref="NotifyCollectionChangedAction.Replace"/> action occurred
        /// </summary>
        int OldStartingIndex { get; }

        /// <summary>
        /// Converts this <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> to a <see cref="NotifyCollectionChangedEventArgs"/>
        /// </summary>
        NotifyCollectionChangedEventArgs ToNotifyCollectionChangedEventArgs();
    }
}
