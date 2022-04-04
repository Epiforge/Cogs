namespace Cogs.ActiveQuery.Tests;

[TestClass]
public class ActiveEnumerable
{
    #region Helper Classes

    class SimpleCollection<T> : ObservableCollection<T>, ISynchronized
    {
        public SynchronizationContext SynchronizationContext => throw new NotImplementedException();
    }

    class SimpleGenericCollection<T> : Collection<T>, INotifyCollectionChanged, ISynchronized
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
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
