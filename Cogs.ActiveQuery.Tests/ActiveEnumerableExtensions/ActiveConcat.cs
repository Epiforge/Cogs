namespace Cogs.ActiveQuery.Tests.ActiveEnumerableExtensions;

[TestClass]
public class ActiveConcat
{
    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void DifferentSynchronizationContexts()
    {
        var left = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        var right = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        using var query = left.ActiveConcat(right);
    }

    [TestMethod]
    public void SourceManipulationLeftContext()
    {
        var left = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        var right = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        Assert.IsNotNull(left.SynchronizationContext);
        using var query = left.ActiveConcat(right, left.SynchronizationContext!);
        Assert.AreEqual(0, query.GetElementFaults().Count);
        Assert.AreEqual(28, query.Count);
        left.RemoveAt(0);
        Assert.AreEqual(27, query.Count);
        right.RemoveRange(12, 2);
        Assert.AreEqual(25, query.Count);
        left[0] = left[0];
        left.ReplaceRange(0, 2, left.GetRange(0, 2));
        left.Add(left[0]);
        Assert.AreEqual(26, query.Count);
        left.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(12, query.Count);
        right[0] = right[0];
        right.ReplaceRange(0, 2, right.GetRange(0, 2));
        right.Add(right[0]);
        Assert.AreEqual(13, query.Count);
        right.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(0, query.Count);
    }

    [TestMethod]
    public void SourceManipulationLeftDoesNotNotify()
    {
        var left = TestPerson.MakePeople();
        var right = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        using var query = left.ActiveConcat(right);
        Assert.AreEqual(0, query.GetElementFaults().Count);
        Assert.AreEqual(28, query.Count);
        right[0] = right[0];
        right.ReplaceRange(0, 2, right.GetRange(0, 2));
        right.Add(right[0]);
        Assert.AreEqual(29, query.Count);
        right.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(14, query.Count);
    }

    [TestMethod]
    public void SourceManipulationRightContext()
    {
        var left = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        var right = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        Assert.IsNotNull(right.SynchronizationContext);
        using var query = left.ActiveConcat(right, right.SynchronizationContext!);
        Assert.AreEqual(0, query.GetElementFaults().Count);
        Assert.AreEqual(28, query.Count);
        left.RemoveAt(0);
        Assert.AreEqual(27, query.Count);
        right.RemoveRange(12, 2);
        Assert.AreEqual(25, query.Count);
        left[0] = left[0];
        left.ReplaceRange(0, 2, left.GetRange(0, 2));
        left.Add(left[0]);
        Assert.AreEqual(26, query.Count);
        left.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(12, query.Count);
        right[0] = right[0];
        right.ReplaceRange(0, 2, right.GetRange(0, 2));
        right.Add(right[0]);
        Assert.AreEqual(13, query.Count);
        right.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(0, query.Count);
    }

    [TestMethod]
    public void SourceManipulationRightDoesNotNotify()
    {
        var left = TestPerson.CreatePeopleCollection(new AsyncSynchronizationContext());
        var right = TestPerson.MakePeople();
        using var query = left.ActiveConcat(right);
        Assert.AreEqual(0, query.GetElementFaults().Count);
        Assert.AreEqual(28, query.Count);
        left.RemoveAt(0);
        Assert.AreEqual(27, query.Count);
        left[0] = left[0];
        left.ReplaceRange(0, 2, left.GetRange(0, 2));
        left.Add(left[0]);
        Assert.AreEqual(28, query.Count);
        left.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(14, query.Count);
    }

    [TestMethod]
    public void SourceManipulationSameContext()
    {
        var synchronizationContext = new AsyncSynchronizationContext();
        var left = TestPerson.CreatePeopleCollection(synchronizationContext);
        var right = TestPerson.CreatePeopleCollection(synchronizationContext);
        using var query = left.ActiveConcat(right);
        Assert.AreEqual(0, query.GetElementFaults().Count);
        Assert.AreEqual(28, query.Count);
        left.RemoveAt(0);
        Assert.AreEqual(27, query.Count);
        right.RemoveRange(12, 2);
        Assert.AreEqual(25, query.Count);
        left[0] = left[0];
        left.ReplaceRange(0, 2, left.GetRange(0, 2));
        left.Add(left[0]);
        Assert.AreEqual(26, query.Count);
        left.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(12, query.Count);
        right[0] = right[0];
        right.ReplaceRange(0, 2, right.GetRange(0, 2));
        right.Add(right[0]);
        Assert.AreEqual(13, query.Count);
        right.Reset(System.Linq.Enumerable.Empty<TestPerson>());
        Assert.AreEqual(0, query.Count);
    }
}
