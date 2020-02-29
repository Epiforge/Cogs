using System;

namespace Cogs.Collections
{
    /// <summary>
    /// Notifies listeners of dynamic changes, such as when a value is added and removed or the whole dictionary is cleared
    /// </summary>
    public interface INotifyDictionaryChanged
    {
        /// <summary>
        /// Occurs when the dictionary changes
        /// </summary>
        event EventHandler<NotifyDictionaryChangedEventArgs<object?, object?>>? DictionaryChanged;
    }

    /// <summary>
    /// Notifies listeners of dynamic changes, such as when a value is added and removed or the whole dictionary is cleared
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary's keys</typeparam>
    /// <typeparam name="TValue">The type of the dictionary's values</typeparam>
    public interface INotifyDictionaryChanged<TKey, TValue>
    {
        /// <summary>
        /// Occurs when the dictionary changes
        /// </summary>
        event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>>? DictionaryChanged;
    }
}
