using Cogs.Components;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Cogs.Collections
{
    /// <summary>
    /// Represents a thread-safe collection of key/value pairs that can be accessed by multiple threads concurrently
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    public class ObservableConcurrentDictionary<TKey, TValue> : PropertyChangeNotifier, ICollection, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable, IEnumerable<KeyValuePair<TKey, TValue>>, IDictionary, IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyGenericCollectionChanged<KeyValuePair<TKey, TValue>>, INotifyDictionaryChanged, INotifyDictionaryChanged<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that is empty, has the default concurrency level, has the default initial capacity, and uses the default comparer for the key type
        /// </summary>
        public ObservableConcurrentDictionary() => cd = new ConcurrentDictionary<TKey, TValue>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IEnumerable{KeyValuePair}"/>, has the default concurrency level, has the default initial capacity, and uses the default comparer for the key type
        /// </summary>
        /// <param name="collection">The <see cref="IEnumerable{KeyValuePair}"/> whose elements are copied to the new <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> or any of its keys is <c>null</c></exception>
        /// <exception cref="ArgumentException"><paramref name="collection"/> contains one or more duplicate keys</exception>
        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) => cd = new ConcurrentDictionary<TKey, TValue>(collection);

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that is empty, has the default concurrency level and capacity, and uses the specified <see cref="IEqualityComparer{TKey}"/>
        /// </summary>
        /// <param name="comparer">The equality comparison implementation to use when comparing keys</param>
        /// <exception cref="ArgumentException"><paramref name="comparer"/> is <c>null</c></exception>
        public ObservableConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            cd = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IEnumerable"/> has the default concurrency level, has the default initial capacity, and uses the specified <see cref="IEqualityComparer{TKey}"/>
        /// </summary>
        /// <param name="collection">The <see cref="IEnumerable{KeyValuePair}"/> whose elements are copied to the new <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="comparer"/> is null</exception>
        public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            ConcurrentBag
            this.comparer = comparer;
            cd = new ConcurrentDictionary<TKey, TValue>(collection, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that is empty, has the specified concurrency level and capacity, and uses the default comparer for the key type
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> concurrently</param>
        /// <param name="capacity">The initial number of elements that the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> can contain</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is less than 1 -or- <paramref name="capacity"/> is less than 0</exception>
        public ObservableConcurrentDictionary(int concurrencyLevel, int capacity)
        {
            this.concurrencyLevel = concurrencyLevel;
            this.capacity = capacity;
            cd = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that contains elements copied from the specified <see cref="IEnumerable"/>, and uses the specified <see cref="IEqualityComparer{TKey}"/>
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> concurrently</param>
        /// <param name="collection">The <see cref="IEnumerable{KeyValuePair}"/> whose elements are copied to the new <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="comparer"/> is <c>null</c></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> is less than 1</exception>
        /// <exception cref="ArgumentException"><paramref name="collection"/> contains one or more duplicate keys</exception>
        public ObservableConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            this.concurrencyLevel = concurrencyLevel;
            this.comparer = comparer;
            cd = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, collection, comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> class that is empty, has the specified concurrency level, has the specified initial capacity, and uses the specified <see cref="IEqualityComparer{TKey}"/>
        /// </summary>
        /// <param name="concurrencyLevel">The estimated number of threads that will update the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> concurrently</param>
        /// <param name="capacity">The initial number of elements that the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> can contain</param>
        /// <param name="comparer">The <see cref="IEqualityComparer{TKey}"/> implementation to use when comparing keys</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c></exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="concurrencyLevel"/> or <paramref name="capacity"/> is less than 1</exception>
        public ObservableConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
        {
            this.concurrencyLevel = concurrencyLevel;
            this.capacity = capacity;
            this.comparer = comparer;
            cd = new ConcurrentDictionary<TKey, TValue>(concurrencyLevel, capacity, comparer);
        }

        readonly int? capacity;
        ConcurrentDictionary<TKey, TValue> cd;
        readonly IEqualityComparer<TKey>? comparer;
        readonly int? concurrencyLevel;

        /// <summary>
        /// Gets or sets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key of the value to get or set</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        /// <exception cref="KeyNotFoundException">The property is retrieved and <paramref name="key"/> does not exist in the collection</exception>
        public virtual TValue this[TKey key]
        {
            get => cd[key];
            set
            {
                var updated = false;
                TValue oldValue = default;
                var newValue = cd.AddOrUpdate(key, k => throw new KeyNotFoundException(), (k, v) =>
                {
                    updated = true;
                    oldValue = v;
                    return value;
                });
                if (updated)
                    OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, newValue, oldValue!));
            }
        }

        object IDictionary.this[object key]
        {
            get => ((IDictionary)cd)[key];
            set
            {
                if (key is null)
                    throw new ArgumentNullException(nameof(key));
                if (key is TKey typedKey)
                {
                    TValue typedValue;
                    try
                    {
                        typedValue = (TValue)value;
                    }
                    catch (InvalidCastException)
                    {
                        throw new ArgumentException("type of value is incorrect");
                    }
                    this[typedKey] = typedValue;
                }
                else
                    throw new ArgumentException("type of key is incorrect", nameof(key));
            }
        }

        /// <summary>
        /// Gets the equality comparison implementation used when comparing keys
        /// </summary>
        public virtual IEqualityComparer<TKey> Comparer => comparer ?? EqualityComparer<TKey>.Default;

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        public virtual int Count => cd.Count;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> is empty
        /// </summary>
        public virtual bool IsEmpty => cd.IsEmpty;

        bool IDictionary.IsFixedSize => ((IDictionary)cd).IsFixedSize;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)cd).IsReadOnly;

        bool IDictionary.IsReadOnly => ((IDictionary)cd).IsReadOnly;

        bool ICollection.IsSynchronized => ((ICollection)cd).IsSynchronized;

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        public virtual ICollection<TKey> Keys => cd.Keys;

        ICollection IDictionary.Keys => ((IDictionary)cd).Keys;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)cd).Keys;

        object ICollection.SyncRoot => ((ICollection)cd).SyncRoot;

        /// <summary>
        /// Gets a collection that contains the values in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        public virtual ICollection<TValue> Values => cd.Values;

        ICollection IDictionary.Values => ((IDictionary)cd).Values;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)cd).Values;

        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Occurs when the dictionary changes
        /// </summary>
        public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;

        event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? INotifyDictionaryChanged.DictionaryChanged
        {
            add => DictionaryChangedBoxed += value;
            remove => DictionaryChangedBoxed -= value;
        }

        event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChangedBoxed;

        /// <summary>
        /// Occurs when the collection changes
        /// </summary>
        public event NotifyGenericCollectionChangedEventHandler<KeyValuePair<TKey, TValue>>? GenericCollectionChanged;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)this).Add(item.Key, item.Value);

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
                throw new ArgumentException("key already exists", nameof(key));
        }

        void IDictionary.Add(object key, object value)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key is TKey typedKey)
            {
                TValue typedValue;
                try
                {
                    typedValue = (TValue)value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("type of value is incorrect", nameof(value));
                }
                ((IDictionary<TKey, TValue>)this).Add(typedKey, typedValue);
            }
            else
                throw new ArgumentException("type of key is incorrect", nameof(key));
        }

        /// <summary>
        /// Uses the specified functions and argument to add a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key does not already exist, or to update a key/value pair in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key already exists
        /// </summary>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/></typeparam>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value</param>
        /// <param name="factoryArgument">An argument to pass into <paramref name="addValueFactory"/> and <paramref name="updateValueFactory"/></param>
        /// <returns>The new value for the key (this will be either be the result of <paramref name="addValueFactory"/> if the key was absent or the result of <paramref name="updateValueFactory"/> if the key was present)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="addValueFactory"/>, or <paramref name="updateValueFactory"/> is a null reference</exception>
        /// <exception cref="OverflowException">The dictionary contains too many elements</exception>
        public virtual TValue AddOrUpdate<TArg>(TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            var updated = false;
            TValue oldValue = default;
            var newValue = cd.AddOrUpdate(key, addValueFactory, (k, v, a) =>
            {
                updated = true;
                oldValue = v;
                return updateValueFactory(k, v, a);
            }, factoryArgument);
            if (updated)
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, newValue, oldValue!));
            else
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, newValue));
            }
            return newValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key does not already exist, or updates a key/value pair in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> by using the specified function if the key already exists
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValue">The value to be added for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value</param>
        /// <returns>The new value for the key (this will be either be <paramref name="addValue"/> if the key was absent or the result of <paramref name="updateValueFactory"/> if the key was present)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="updateValueFactory"/> is <c>null</c></exception>
        /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
        public virtual TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            var updated = false;
            TValue oldValue = default;
            var newValue = cd.AddOrUpdate(key, addValue, (k, v) =>
            {
                updated = true;
                oldValue = v;
                return updateValueFactory(k, v);
            });
            if (updated)
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, newValue, oldValue!));
            else
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, newValue));
            }
            return newValue;
        }

        /// <summary>
        /// Uses the specified functions to add a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key does not already exist, or to update a key/value pair in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key already exists
        /// </summary>
        /// <param name="key">The key to be added or whose value should be updated</param>
        /// <param name="addValueFactory">The function used to generate a value for an absent key</param>
        /// <param name="updateValueFactory">The function used to generate a new value for an existing key based on the key's existing value</param>
        /// <returns>The new value for the key (this will be either be the result of <paramref name="addValueFactory"/> if the key was absent or the result of <paramref name="updateValueFactory"/> if the key was present)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/>, <paramref name="addValueFactory"/>, or <paramref name="updateValueFactory"/> is <c>null</c></exception>
        /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
        public virtual TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            var updated = false;
            TValue oldValue = default;
            var newValue = cd.AddOrUpdate(key, addValueFactory, (k, v) =>
            {
                updated = true;
                oldValue = v;
                return updateValueFactory(k, v);
            });
            if (updated)
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, newValue, oldValue!));
            else
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, newValue));
            }
            return newValue;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        public virtual void Clear()
        {
            var currentCd = cd;
            if (comparer is { })
            {
                if (concurrencyLevel is { } cl)
                {
                    if (capacity is { } c)
                        cd = new ConcurrentDictionary<TKey, TValue>(cl, c, comparer);
                    else
                        cd = new ConcurrentDictionary<TKey, TValue>(cl, Enumerable.Empty<KeyValuePair<TKey, TValue>>(), comparer);
                }
                else
                    cd = new ConcurrentDictionary<TKey, TValue>(comparer);
            }
            else if (concurrencyLevel is { } cl && capacity is { } c)
                cd = new ConcurrentDictionary<TKey, TValue>(cl, c);
            else
                cd = new ConcurrentDictionary<TKey, TValue>();
            NotifyCountChanged();
            OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, Enumerable.Empty<KeyValuePair<TKey, TValue>>(), currentCd.ToImmutableArray()));
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)cd).Contains(item);

        bool IDictionary.Contains(object key) => ((IDictionary)cd).Contains(key);

        /// <summary>
        /// Determines whether the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> contains the specified key
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></param>
        /// <returns><c>true</c> if the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, <c>false</c></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        public virtual bool ContainsKey(TKey key) => cd.ContainsKey(key);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)cd).CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index) => ((ICollection)cd).CopyTo(array, index);

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        /// <returns>An enumerator for the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></returns>
        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => cd.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)cd).GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)cd).GetEnumerator();

        /// <summary>
        /// Adds a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> by using the specified function if the key does not already exist (returns the new value, or the existing value if the key exists)
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <returns>The value for the key (this will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="valueFactory"/> is <c>null</c></exception>
        /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            var added = false;
            var retrievedOrAddedValue = cd.GetOrAdd(key, k =>
            {
                added = true;
                return valueFactory(k);
            });
            if (added)
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, retrievedOrAddedValue));
            }
            return retrievedOrAddedValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> if the key does not already exist (returns the new value, or the existing value if the key exists)
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value to be added, if the key does not already exist</param>
        /// <returns>The value for the key (this will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
        public virtual TValue GetOrAdd(TKey key, TValue value)
        {
            var added = false;
            var retrievedOrAddedValue = cd.GetOrAdd(key, k =>
            {
                added = true;
                return value;
            });
            if (added)
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, retrievedOrAddedValue));
            }
            return retrievedOrAddedValue;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> by using the specified function and an argument if the key does not already exist, or returns the existing value if the key exists
        /// </summary>
        /// <typeparam name="TArg">The type of an argument to pass into <paramref name="valueFactory"/></typeparam>
        /// <param name="key">The key of the element to add</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <param name="factoryArgument">An argument value to pass into <paramref name="valueFactory"/></param>
        /// <returns>The value for the key (this will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary)</returns>
        /// <exception cref="ArgumentNullException">key is a null reference</exception>
        /// <exception cref="OverflowException">The dictionary contains too many elements</exception>
        public virtual TValue GetOrAdd<TArg>(TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            var added = false;
            var retrievedOrAddedValue = cd.GetOrAdd(key, (k, a) =>
            {
                added = true;
                return valueFactory(k, a);
            }, factoryArgument);
            if (added)
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, retrievedOrAddedValue));
            }
            return retrievedOrAddedValue;
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for the <see cref="Count"/> property
        /// </summary>
        protected virtual void NotifyCountChanged() => OnPropertyChanged(nameof(Count));

        /// <summary>
        /// Calls <see cref="OnDictionaryChanged(NotifyDictionaryChangedEventArgs{TKey, TValue})"/> and also calls <see cref="OnCollectionChanged(NotifyCollectionChangedEventArgs)"/>, <see cref="OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs{object, object})"/>, and <see cref="OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs{KeyValuePair{TKey, TValue}})"/> when applicable
        /// </summary>
        /// <param name="e">The event arguments for <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/></param>
        protected virtual void OnChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e)
        {
            if (CollectionChanged is { })
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            if (GenericCollectionChanged is { })
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Add, e.NewItems));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Remove, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Replace, e.NewItems, e.OldItems));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnGenericCollectionChanged(new NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>(NotifyCollectionChangedAction.Reset));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            if (DictionaryChangedBoxed is { })
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Add:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Add, e.NewItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Remove:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Remove, e.OldItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Replace:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Replace, e.NewItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value)), e.OldItems.Select(kv => new KeyValuePair<object?, object?>(kv.Key, kv.Value))));
                        break;
                    case NotifyDictionaryChangedAction.Reset:
                        OnDictionaryChangedBoxed(new NotifyDictionaryChangedEventArgs<object?, object?>(NotifyDictionaryChangedAction.Reset));
                        break;
                }
            OnDictionaryChanged(e);
        }

        /// <summary>
        /// Raises the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e) => DictionaryChanged?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="INotifyDictionaryChanged.DictionaryChanged"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnDictionaryChangedBoxed(NotifyDictionaryChangedEventArgs<object?, object?> e) => DictionaryChangedBoxed?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnGenericCollectionChanged(NotifyGenericCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e) => GenericCollectionChanged?.Invoke(this, e);

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
            TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(item.Value, value) && TryRemove(item.Key, out _);

        void IDictionary.Remove(object key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));
            if (key is TKey typedKey)
                ((IDictionary<TKey, TValue>)this).Remove(typedKey);
            else
                throw new ArgumentException("type of key is incorrect", nameof(key));
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key) => TryRemove(key, out _);

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> to a new array
        /// </summary>
        /// <returns>A new array containing a snapshot of key and value pairs copied from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/></returns>
        public virtual KeyValuePair<TKey, TValue>[] ToArray() => cd.ToArray();

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="key">The key of the element to add</param>
        /// <param name="value">The value of the element to add (the value can be null for reference types)</param>
        /// <returns><c>true</c> if the key/value pair was added to the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> successfully; <c>false</c> if the key already exists</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        /// <exception cref="OverflowException">The dictionary already contains the maximum number of elements (<see cref="int.MaxValue"/>)</exception>
        public virtual bool TryAdd(TKey key, TValue value)
        {
            if (cd.TryAdd(key, value))
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, key, value));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="value">When this method returns, contains the object from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> that has the specified key, or the default value of the type if the operation failed</param>
        /// <returns><c>true</c> if the key was found in the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>; otherwise, <c>false</c></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        public virtual bool TryGetValue(TKey key, out TValue value) => cd.TryGetValue(key, out value);

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="key">The key of the element to remove and return</param>
        /// <param name="value">When this method returns, contains the object removed from the <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>, or the default value of the <typeparamref name="TValue"/> type if key does not exist</param>
        /// <returns><c>true</c> if the object was removed successfully; otherwise, <c>false</c></returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c></exception>
        public virtual bool TryRemove(TKey key, out TValue value)
        {
            if (cd.TryRemove(key, out value))
            {
                NotifyCountChanged();
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, key, value));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the value associated with key to newValue if the existing value with key is equal to comparisonValue
        /// </summary>
        /// <param name="key">The key of the value that is compared with comparisonValue and possibly replaced</param>
        /// <param name="newValue">The value that replaces the value of the element that has the specified key if the comparison results in equality</param>
        /// <param name="comparisonValue">The value that is compared with the value of the element that has the specified key</param>
        /// <returns>true if the value with key was equal to comparisonValue and was replaced with</returns>
        public virtual bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            try
            {
                TValue oldValue = default;
                cd.AddOrUpdate(key, k => throw new KeyNotFoundException(), (k, v) =>
                {
                    if (EqualityComparer<TValue>.Default.Equals(v, comparisonValue))
                    {
                        oldValue = v;
                        return newValue;
                    }
                    throw new ValueComparisonUnequalException();
                });
                OnChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Replace, key, newValue, oldValue!));
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            catch (ValueComparisonUnequalException)
            {
                return false;
            }
        }

        /// <summary>
        /// Represents when the valueFactory used by <see cref="TryUpdate(TKey, TValue, TValue)"/> finds the oldValue and comparisonValue are unequal
        /// </summary>
        protected class ValueComparisonUnequalException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
            /// </summary>
            public ValueComparisonUnequalException() : base("the oldValue and comparisonValue are unequal")
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
            /// </summary>
            /// <param name="message">The message that describes the error</param>
            public ValueComparisonUnequalException(string message) : base(message)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ValueComparisonUnequalException"/> class
            /// </summary>
            /// <param name="message">The message that describes the error</param>
            /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified</param>
            public ValueComparisonUnequalException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
}
