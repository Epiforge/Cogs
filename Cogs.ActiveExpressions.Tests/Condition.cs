namespace Cogs.ActiveExpressions.Tests;

[TestClass]
public class Condition
{
    [TestMethod]
    public void AlreadyCancelled()
    {
        var john = TestPerson.CreateJohn();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var johnsNameIsSixCharacters = ActiveExpression.ConditionAsync(() => john.Name!.Length == 6, cancellationTokenSource.Token);
        Assert.IsTrue(johnsNameIsSixCharacters.IsCompleted);
        Assert.IsTrue(johnsNameIsSixCharacters.IsCanceled);
    }

    [TestMethod]
    public void AlreadyFaulted()
    {
        var john = TestPerson.CreateJohn();
        john.Name = null;
        var johnsNameIsSixCharacters = ActiveExpression.ConditionAsync(() => john.Name!.Length == 6);
        Assert.IsTrue(johnsNameIsSixCharacters.IsCompleted);
        Assert.IsInstanceOfType(johnsNameIsSixCharacters.Exception, typeof(AggregateException));
        Assert.IsInstanceOfType(johnsNameIsSixCharacters.Exception!.InnerExceptions[0], typeof(NullReferenceException));
    }

    [TestMethod]
    public void AlreadySucceeded()
    {
        var john = TestPerson.CreateJohn();
        john.Name = "Jan";
        var johnsSexChange = ActiveExpression.ConditionAsync(() => john.Name == "Jan");
        Assert.IsTrue(johnsSexChange.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void CancelledLater()
    {
        var john = TestPerson.CreateJohn();
        using var cancellationTokenSource = new CancellationTokenSource();
        var johnsNameIsSixCharacters = ActiveExpression.ConditionAsync(() => john.Name!.Length == 6, cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();
        Assert.IsTrue(johnsNameIsSixCharacters.IsCompleted);
        Assert.IsTrue(johnsNameIsSixCharacters.IsCanceled);
    }

    [TestMethod]
    public void Fault()
    {
        var john = TestPerson.CreateJohn();
        var johnsNameIsSixCharacters = ActiveExpression.ConditionAsync(() => john.Name!.Length == 6);
        Assert.IsFalse(johnsNameIsSixCharacters.IsCompleted);
        john.Name = null;
        Assert.IsTrue(johnsNameIsSixCharacters.IsCompleted);
        Assert.IsInstanceOfType(johnsNameIsSixCharacters.Exception, typeof(AggregateException));
        Assert.IsInstanceOfType(johnsNameIsSixCharacters.Exception!.InnerExceptions[0], typeof(NullReferenceException));
    }

    [TestMethod]
    public void Success()
    {
        var john = TestPerson.CreateJohn();
        var johnsSexChange = ActiveExpression.ConditionAsync(() => john.Name == "Jan");
        Assert.IsFalse(johnsSexChange.IsCompleted);
        john.Name = "Jon";
        Assert.IsFalse(johnsSexChange.IsCompleted);
        john.Name = "Jan";
        Assert.IsTrue(johnsSexChange.IsCompletedSuccessfully);
    }
}
