using Cogs.Collections;
using Cogs.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;

namespace Cogs.ActiveQuery.Tests
{
    [TestClass]
    public class ActiveEnumerable
    {
        #region Helper Classes

        class SimpleCollection<T> : ObservableCollection<T>, ISynchronized
        {
            public SynchronizationContext SynchronizationContext => throw new NotImplementedException();
        }

        class SimpleGenericCollection<T> : Collection<T>, INotifyGenericCollectionChanged<T>, ISynchronized
        {
            public event NotifyGenericCollectionChangedEventHandler<T> GenericCollectionChanged;
            protected override void InsertItem(int index, T item)
            {
                base.InsertItem(index, item);
                GenericCollectionChanged?.Invoke(this, new NotifyGenericCollectionChangedEventArgs<T>(NotifyCollectionChangedAction.Add, item, index));
            }

            public SynchronizationContext SynchronizationContext => throw new NotImplementedException();
        }

        #endregion Helper Classes

        [TestMethod]
        public void CollectionChangedOnly()
        {
            var collection = new SimpleCollection<Guid>();
            using var enumerable = new ActiveEnumerable<Guid>(collection);
            collection.Add(default);
        }

        [TestMethod]
        public void GenericCollectionChangedOnly()
        {
            var collection = new SimpleGenericCollection<Guid>();
            using var enumerable = new ActiveEnumerable<Guid>(collection);
            collection.Add(default);
        }
    }
}
