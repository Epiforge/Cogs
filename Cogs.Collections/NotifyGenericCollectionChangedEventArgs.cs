using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace Cogs.Collections
{
    /// <summary>
    /// Provides data for the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    public class NotifyGenericCollectionChangedEventArgs<T> : EventArgs, INotifyGenericCollectionChangedEventArgs<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a <see cref="NotifyCollectionChangedAction.Reset"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyCollectionChangedAction.Reset"/>)</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            if (action != NotifyCollectionChangedAction.Reset)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeAdd(action, null, -1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a one-item change
        /// </summary>
        /// <param name="action">The action that caused the event (this can be set to <see cref="NotifyCollectionChangedAction.Reset"/>, <see cref="NotifyCollectionChangedAction.Add"/>, or <see cref="NotifyCollectionChangedAction.Remove"/>)</param>
        /// <param name="changedItem">The item that is affected by the change</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItem != null)
                        throw new ArgumentException(nameof(changedItem));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    InitializeAddOrRemove(action, new T[] { changedItem }, -1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a one-item change
        /// </summary>
        /// <param name="action">The action that caused the event (this can be set to <see cref="NotifyCollectionChangedAction.Reset"/>, <see cref="NotifyCollectionChangedAction.Add"/>, or <see cref="NotifyCollectionChangedAction.Remove"/>)</param>
        /// <param name="changedItem">The item that is affected by the change</param>
        /// <param name="index">The index where the change occurred</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem, int index)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItem != null)
                        throw new ArgumentException(nameof(changedItem));
                    if (index != -1)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    InitializeAddOrRemove(action, new T[] { changedItem }, index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a multi-item change
        /// </summary>
        /// <param name="action">The action that caused the event (this can be set to <see cref="NotifyCollectionChangedAction.Reset"/>, <see cref="NotifyCollectionChangedAction.Add"/>, or <see cref="NotifyCollectionChangedAction.Remove"/>)</param>
        /// <param name="changedItems">The items that are affected by the change</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T>? changedItems)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItems != null)
                        throw new ArgumentException(nameof(changedItems));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (changedItems == null)
                        throw new ArgumentNullException(nameof(changedItems));
                    InitializeAddOrRemove(action, changedItems, -1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a multi-item change or a <see cref="NotifyCollectionChangedAction.Reset"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can be set to <see cref="NotifyCollectionChangedAction.Reset"/>, <see cref="NotifyCollectionChangedAction.Add"/>, or <see cref="NotifyCollectionChangedAction.Remove"/>)</param>
        /// <param name="changedItems">The items affected by the change</param>
        /// <param name="startingIndex">The index where the change occurred</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T>? changedItems, int startingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItems != null)
                        throw new ArgumentException(nameof(changedItems));
                    if (startingIndex != -1)
                        throw new ArgumentOutOfRangeException(nameof(startingIndex));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (changedItems == null)
                        throw new ArgumentNullException(nameof(changedItems));
                    if (startingIndex < -1)
                        throw new ArgumentOutOfRangeException(nameof(startingIndex));
                    InitializeAddOrRemove(action, changedItems, startingIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a one-item <see cref="NotifyCollectionChangedAction.Replace"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Replace"/>)</param>
        /// <param name="newItem">The new item that is replacing the original item</param>
        /// <param name="oldItem">The original item that is replaced</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T newItem, T oldItem)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, -1, -1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a one-item <see cref="NotifyCollectionChangedAction.Replace"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Replace"/>)</param>
        /// <param name="newItem">The new item that is replacing the original item</param>
        /// <param name="oldItem">The original item that is replaced</param>
        /// <param name="index">The index of the item being replaced</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T newItem, T oldItem, int index)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, index, index);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a multi-item <see cref="NotifyCollectionChangedAction.Replace"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Replace"/>)</param>
        /// <param name="newItems">The new items that are replacing the original items</param>
        /// <param name="oldItems">The original items that are replaced</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T>? newItems, IEnumerable<T>? oldItems)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));
            if (oldItems == null)
                throw new ArgumentNullException(nameof(oldItems));
            InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a multi-item <see cref="NotifyCollectionChangedAction.Replace"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Replace"/>)</param>
        /// <param name="newItems">The new items that are replacing the original items</param>
        /// <param name="oldItems">The original items that are replaced</param>
        /// <param name="startingIndex">The index of the first item of the items that are being replaced</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T>? newItems, IEnumerable<T>? oldItems, int startingIndex)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));
            if (oldItems == null)
                throw new ArgumentNullException(nameof(oldItems));
            InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> class that describes a one-item <see cref="NotifyCollectionChangedAction.Move"/> change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Move"/>)</param>
        /// <param name="changedItem">The item affected by the change</param>
        /// <param name="index">The new index for the changed item</param>
        /// <param name="oldIndex">The old index for the changed item</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var changedItems = new T[] { changedItem };
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        /// <summary>
        /// Initializes a new instance of the NotifyCollectionChangedEventArgs class that describes a multi-item Move change
        /// </summary>
        /// <param name="action">The action that caused the event (this can only be set to <see cref="NotifyCollectionChangedAction.Move"/>)</param>
        /// <param name="changedItems">The items affected by the change</param>
        /// <param name="index">The new index for the changed items</param>
        /// <param name="oldIndex">The old index for the changed items</param>
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T>? changedItems, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        void InitializeAdd(NotifyCollectionChangedAction action, IEnumerable<T>? newItems, int newStartingIndex)
        {
            Action = action;
            if (newItems is IEnumerable<T> actualNewItems)
                NewItems = actualNewItems.ToImmutableArray();
            NewStartingIndex = newStartingIndex;
        }

        void InitializeAddOrRemove(NotifyCollectionChangedAction action, IEnumerable<T>? changedItems, int startingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    InitializeAdd(action, changedItems, startingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    InitializeRemove(action, changedItems, startingIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
               
        void InitializeMoveOrReplace(NotifyCollectionChangedAction action, IEnumerable<T>? newItems, IEnumerable<T>? oldItems, int startingIndex, int oldStartingIndex)
        {
            InitializeAdd(action, newItems, startingIndex);
            InitializeRemove(action, oldItems, oldStartingIndex);
        }

        void InitializeRemove(NotifyCollectionChangedAction action, IEnumerable<T>? oldItems, int oldStartingIndex)
        {
            Action = action;
            if (oldItems is IEnumerable<T> actualOldItems)
                OldItems = actualOldItems.ToImmutableArray();
            OldStartingIndex = oldStartingIndex;
        }

        /// <summary>
        /// Gets the action that caused the event
        /// </summary>
        public NotifyCollectionChangedAction Action { get; private set; }

        /// <summary>
        /// Gets the list of new items involved in the change
        /// </summary>
        public IReadOnlyList<T> NewItems { get; private set; } = Enumerable.Empty<T>().ToImmutableArray();

        /// <summary>
        /// Gets the index at which the change occurred
        /// </summary>
        public int NewStartingIndex { get; private set; }

        /// <summary>
        /// Gets the list of items affected by a <see cref="NotifyCollectionChangedAction.Replace"/>, <see cref="NotifyCollectionChangedAction.Remove"/>, or <see cref="NotifyCollectionChangedAction.Move"/> action
        /// </summary>
        public IReadOnlyList<T> OldItems { get; private set; } = Enumerable.Empty<T>().ToImmutableArray();

        /// <summary>
        /// Gets the index at which a <see cref="NotifyCollectionChangedAction.Move"/>, <see cref="NotifyCollectionChangedAction.Remove"/>, or <see cref="NotifyCollectionChangedAction.Replace"/> action occurred
        /// </summary>
        public int OldStartingIndex { get; private set; }

        /// <summary>
        /// Defines an explicit conversion of a <see cref="NotifyCollectionChangedEventArgs"/> to a <see cref="NotifyGenericCollectionChangedEventArgs{T}"/>
        /// </summary>
        /// <param name="e">The <see cref="NotifyCollectionChangedEventArgs"/></param>
        public static explicit operator NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedEventArgs e)
        {
            return e.Action switch
            {
                NotifyCollectionChangedAction.Add => new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Add, e.NewItems.Cast<T>().ToImmutableArray(), e.NewStartingIndex),
                NotifyCollectionChangedAction.Move => new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Move, (e.NewItems ?? e.OldItems).Cast<T>().ToImmutableArray(), e.NewStartingIndex, e.OldStartingIndex),
                NotifyCollectionChangedAction.Remove => new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Remove, e.OldItems.Cast<T>().ToImmutableArray(), e.OldStartingIndex),
                NotifyCollectionChangedAction.Replace => new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Replace, e.NewItems.Cast<T>().ToImmutableArray(), e.OldItems.Cast<T>().ToImmutableArray(), e.NewStartingIndex),
                NotifyCollectionChangedAction.Reset => new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Reset),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        /// Defines an implicit conversion of a <see cref="NotifyGenericCollectionChangedEventArgs{T}"/> to a <see cref="NotifyCollectionChangedEventArgs"/>
        /// </summary>
        /// <param name="e">The <see cref="NotifyGenericCollectionChangedEventArgs{T}"/></param>
        public static implicit operator NotifyCollectionChangedEventArgs(NotifyGenericCollectionChangedEventArgs<T> e)
        {
            return e.Action switch
            {
                NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems, e.NewStartingIndex),
                NotifyCollectionChangedAction.Move => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, e.NewItems, e.NewStartingIndex, e.OldStartingIndex),
                NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems, e.OldStartingIndex),
                NotifyCollectionChangedAction.Replace => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems, e.NewStartingIndex),
                NotifyCollectionChangedAction.Reset => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
