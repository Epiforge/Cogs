namespace Cogs.Collections;

/// <summary>
/// Provides data for the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary</typeparam>
public class NotifyDictionaryChangedEventArgs<TKey, TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a <see cref="NotifyDictionaryChangedAction.Reset"/> change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Reset"/>)</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action)
    {
        if (action != NotifyDictionaryChangedAction.Reset)
            throw new ArgumentOutOfRangeException(nameof(action));
        InitializeAdd(action);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a one-item change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Add"/> or <see cref="NotifyDictionaryChangedAction.Remove"/>)</param>
    /// <param name="key">The key of the item that is affected by the change</param>
    /// <param name="value">The value of the item that is affected by the change</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, TKey key, TValue value) : this(action, new KeyValuePair<TKey, TValue>(key, value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a one-item change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Add"/> or <see cref="NotifyDictionaryChangedAction.Remove"/>)</param>
    /// <param name="changedItem">The item that is affected by the change</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, KeyValuePair<TKey, TValue> changedItem) : this(action, new KeyValuePair<TKey, TValue>[] { changedItem })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a multi-item change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Add"/> or <see cref="NotifyDictionaryChangedAction.Remove"/>)</param>
    /// <param name="changedItems">The items that are affected by the change</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>> changedItems)
    {
        switch (action)
        {
            case NotifyDictionaryChangedAction.Add:
                InitializeAdd(action, changedItems);
                break;
            case NotifyDictionaryChangedAction.Remove:
                InitializeRemove(action, changedItems);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a one-item <see cref="NotifyDictionaryChangedAction.Replace"/> change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Replace"/>)</param>
    /// <param name="key">The key of the item that is affected by the change</param>
    /// <param name="newValue">The new value that is replacing the original value</param>
    /// <param name="oldValue">The original value that is replaced</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, TKey key, TValue newValue, TValue oldValue) : this(action, new KeyValuePair<TKey, TValue>(key, newValue), new KeyValuePair<TKey, TValue>(key, oldValue))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a one-item <see cref="NotifyDictionaryChangedAction.Replace"/> change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Replace"/>)</param>
    /// <param name="newItem">The new key-value pair that is replacing the original key-value pair</param>
    /// <param name="oldItem">The original key-value pair that is replaced</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, KeyValuePair<TKey, TValue> newItem, KeyValuePair<TKey, TValue> oldItem) : this(action, new KeyValuePair<TKey, TValue>[] { newItem }, new KeyValuePair<TKey, TValue>[] { oldItem })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyDictionaryChangedEventArgs{TKey, TValue}"/> class that describes a multi-item <see cref="NotifyDictionaryChangedAction.Replace"/> change
    /// </summary>
    /// <param name="action">The action that caused the event (this must be set to <see cref="NotifyDictionaryChangedAction.Replace"/>)</param>
    /// <param name="newItems">The new key-value pairs that are replacing the original key-value pairs</param>
    /// <param name="oldItems">The original key-value pairs that are replaced</param>
    public NotifyDictionaryChangedEventArgs(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>>? newItems, IEnumerable<KeyValuePair<TKey, TValue>>? oldItems)
    {
        if (action != NotifyDictionaryChangedAction.Replace)
            throw new ArgumentOutOfRangeException(nameof(action));
        InitializeAdd(action, newItems);
        InitializeRemove(action, oldItems);
    }

    void InitializeAdd(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>>? newItems = null)
    {
        Action = action;
        if (newItems is IEnumerable<KeyValuePair<TKey, TValue>> actualNewItems)
            NewItems = actualNewItems.ToImmutableArray();
    }

    void InitializeRemove(NotifyDictionaryChangedAction action, IEnumerable<KeyValuePair<TKey, TValue>>? oldItems)
    {
        Action = action;
        if (oldItems is IEnumerable<KeyValuePair<TKey, TValue>> actualOldItems)
            OldItems = actualOldItems.ToImmutableArray();
    }

    /// <summary>
    /// Gets the action that caused the event
    /// </summary>
    public NotifyDictionaryChangedAction Action { get; private set; }

    /// <summary>
    /// Gets the list of new items involved in the change
    /// </summary>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> NewItems { get; private set; } = Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToImmutableArray();

    /// <summary>
    /// Gets the list of items affected by a <see cref="NotifyDictionaryChangedAction.Replace"/> or <see cref="NotifyDictionaryChangedAction.Remove"/> action
    /// </summary>
    public IReadOnlyList<KeyValuePair<TKey, TValue>> OldItems { get; private set; } = Enumerable.Empty<KeyValuePair<TKey, TValue>>().ToImmutableArray();
}
