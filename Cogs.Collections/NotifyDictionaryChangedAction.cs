namespace Cogs.Collections
{
    /// <summary>
    /// Describes the action that caused a <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event
    /// </summary>
    public enum NotifyDictionaryChangedAction
    {
        /// <summary>
        /// An item was added to the dictionary
        /// </summary>
        Add,

        /// <summary>
        /// An item was removed from the dictionary
        /// </summary>
        Remove,

        /// <summary>
        /// An item was replaced in the dictionary
        /// </summary>
        Replace,

        /// <summary>
        /// The content of the dictionary was cleared
        /// </summary>
        Reset
    }
}
