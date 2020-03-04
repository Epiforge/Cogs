using Cogs.Collections;
using Cogs.Disposal;
using Cogs.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Cogs.ActiveQuery
{
    /// <summary>
    /// Represents a read-only collection of elements that is the result of an active query
    /// </summary>
    /// <typeparam name="TElement">The type of the elements in the sequence</typeparam>
    public interface IActiveEnumerable<out TElement> : IDisposable, IDisposalStatus, INotifyCollectionChanged, INotifyDisposed, INotifyDisposing, INotifyElementFaultChanges, INotifyGenericCollectionChanged<TElement>, INotifyPropertyChanged, INotifyPropertyChanging, IReadOnlyList<TElement>, ISynchronized
    {
    }
}
