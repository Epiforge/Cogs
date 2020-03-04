using Cogs.Collections;
using Cogs.Disposal;
using Cogs.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

namespace Gear.ActiveQuery
{
    /// <summary>
    /// Represents a read-only collection of elements that is the result of an active query
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
    public class ActiveEnumerable<TElement> : SyncDisposable, IActiveEnumerable<TElement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveEnumerable{TElement}"/> class
        /// </summary>
        /// <param name="readOnlyList">The read-only list upon which the <see cref="ActiveEnumerable{TElement}"/> is based</param>
        /// <param name="faultNotifier">The <see cref="INotifyElementFaultChanges"/> for the underlying data of the <see cref="ActiveEnumerable{TElement}"/></param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveEnumerable{TElement}"/> is disposed</param>
        public ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, INotifyElementFaultChanges? faultNotifier = null, Action? onDispose = null)
        {
            synchronized = readOnlyList as ISynchronized ?? throw new ArgumentException($"{nameof(readOnlyList)} must implement {nameof(ISynchronized)}", nameof(readOnlyList));
            this.faultNotifier = faultNotifier ?? (readOnlyList as INotifyElementFaultChanges);
            if (this.faultNotifier != null)
            {
                this.faultNotifier.ElementFaultChanged += FaultNotifierElementFaultChanged;
                this.faultNotifier.ElementFaultChanging += FaultNotifierElementFaultChanging;
            }
            if (readOnlyList is ActiveEnumerable<TElement> activeEnumerable)
                this.readOnlyList = activeEnumerable.readOnlyList;
            else
                this.readOnlyList = readOnlyList;
            if (this.readOnlyList is INotifyCollectionChanged collectionNotifier)
            {
                isCollectionNotifier = true;
                collectionNotifier.CollectionChanged += CollectionChangedHandler;
            }
            if (this.readOnlyList is INotifyGenericCollectionChanged<TElement> genericCollectionNotifier)
            {
                isGenericCollectionNotifier = true;
                genericCollectionNotifier.GenericCollectionChanged += GenericCollectionChangedHandler;
            }
            this.onDispose = onDispose;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveEnumerable{TElement}"/> class
        /// </summary>
        /// <param name="readOnlyList">The read-only list upon which the <see cref="ActiveEnumerable{TElement}"/> is based</param>
        /// <param name="onDispose">The action to take when the <see cref="ActiveEnumerable{TElement}"/> is disposed</param>
        public ActiveEnumerable(IReadOnlyList<TElement> readOnlyList, Action onDispose) : this(readOnlyList, null, onDispose)
        {
        }

        readonly INotifyElementFaultChanges? faultNotifier;
        readonly bool isCollectionNotifier;
        readonly bool isGenericCollectionNotifier;
        readonly Action? onDispose;
        readonly IReadOnlyList<TElement> readOnlyList;
        readonly ISynchronized synchronized;

        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Occurs when the fault for an element has changed
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanged;

        /// <summary>
        /// Occurs when the fault for an element is changing
        /// </summary>
        public event EventHandler<ElementFaultChangeEventArgs>? ElementFaultChanging;

        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        public event NotifyGenericCollectionChangedEventHandler<TElement>? GenericCollectionChanged;

        void CollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
            if (!isGenericCollectionNotifier)
                GenericCollectionChanged?.Invoke(this, (NotifyGenericCollectionChangedEventArgs<TElement>)e);
        }

        void GenericCollectionChangedHandler(object sender, INotifyGenericCollectionChangedEventArgs<TElement> e)
        {
            if (!isCollectionNotifier)
                CollectionChanged?.Invoke(this, (NotifyGenericCollectionChangedEventArgs<TElement>)e);
            GenericCollectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
        protected override bool Dispose(bool disposing)
        {
            if (disposing)
            {
                onDispose?.Invoke();
                if (faultNotifier != null)
                {
                    faultNotifier.ElementFaultChanged -= FaultNotifierElementFaultChanged;
                    faultNotifier.ElementFaultChanging -= FaultNotifierElementFaultChanging;
                }
                if (readOnlyList is INotifyCollectionChanged collectionNotifier)
                    collectionNotifier.CollectionChanged -= CollectionChangedHandler;
                if (readOnlyList is INotifyGenericCollectionChanged<TElement> genericCollectionNotifier)
                    genericCollectionNotifier.GenericCollectionChanged -= GenericCollectionChangedHandler;
            }
            return true;
        }

        void FaultNotifierElementFaultChanged(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanged?.Invoke(this, e);

        void FaultNotifierElementFaultChanging(object sender, ElementFaultChangeEventArgs e) => ElementFaultChanging?.Invoke(this, e);

        IEnumerator IEnumerable.GetEnumerator() => readOnlyList.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the collection
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection</returns>
        public IEnumerator<TElement> GetEnumerator() => readOnlyList.GetEnumerator();

        /// <summary>
        /// Gets a list of all faulted elements
        /// </summary>
        /// <returns>The list</returns>
        public IReadOnlyList<(object? element, Exception? fault)> GetElementFaults() => faultNotifier?.GetElementFaults() ?? Enumerable.Empty<(object? element, Exception? fault)>().ToImmutableArray();

        /// <summary>
        /// Gets the element at the specified index in the read-only list
        /// </summary>
        /// <param name="index">The zero-based index of the element to get</param>
        /// <returns>The element at the specified index in the read-only list</returns>
        public TElement this[int index] => readOnlyList[index];

        /// <summary>
        /// Gets the number of elements in the collection
        /// </summary>
        public int Count => readOnlyList.Count;

        /// <summary>
        /// Gets the <see cref="System.Threading.SynchronizationContext"/> on which this object's operations occur
        /// </summary>
        public SynchronizationContext SynchronizationContext => synchronized.SynchronizationContext;
    }
}
