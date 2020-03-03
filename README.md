![Cogs Logo](Cogs.jpg) 

<h1>Cogs</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard2.1`.

- [Libraries](#libraries)
  - [Active Expressions](#active-expressions)
  - [Collections](#collections)
  - [Components](#components)
  - [Disposal](#disposal)
  - [Reflection](#reflection)
  - [Synchronized Collections](#synchronized-collections)
  - [Threading](#threading)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Libraries

## Active Expressions

[![Cogs.ActiveExpressions Nuget](https://img.shields.io/nuget/v/Cogs.ActiveExpressions.svg)](https://www.nuget.org/packages/Cogs.ActiveExpressions)

This library accepts a `LambdaExpression` and arguments to pass to it, dissects the `LambdaExpression`'s body, and hooks into change notification events for properties (`INotifyPropertyChanged`), collections (`INotifyCollectionChanged`), and dictionaries (`Cogs.Collections.INotifyDictionaryChanged`).

```csharp
var elizabeth = Employee.GetByName("Elizabeth"); // Employee implements INotifyPropertyChanged
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr subscribed to elizabeth's PropertyChanged
```

Then, as changes involving any elements of the expression occur, a chain of automatic re-evaluation will get kicked off, possibly causing the active expression's `Value` property to change.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr.Value == 9
elizabeth.Name = "Lizzy"; // expr.Value == 5
```

Also, since exceptions may be encountered after an active expression was created due to subsequent element changes, active expressions also have a `Fault` property, which will be set to the exception that was encountered during evaluation.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr.Fault is null
elizabeth.Name = null; // expr.Fault is NullReferenceException
```

Active expressions raise property change events of their own, so listen for those (kinda the whole point)!

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth);
expr.PropertyChanged += (sender, e) =>
{
    if (e.PropertyName == "Fault")
    {
        // Whoops
    }
    else if (e.PropertyName == "Value")
    {
        // Do something
    }
};
```

When you dispose of your active expression, it will disconnect from all the events.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
using (var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth))
{
    // expr subscribed to elizabeth's PropertyChanged
}
// expr unsubcribed from elizabeth's PropertyChanged
```

Active expressions will also try to automatically dispose of disposable objects they create in the course of their evaluation when and where it makes sense.
Use the `ActiveExpressionOptions` class for more direct control over this behavior.

You can use the static property `Optimizer` to specify an optimization method to invoke automatically during the active expression creation process.
We recommend Tuomas Hietanen's [Linq.Expression.Optimizer](https://thorium.github.io/Linq.Expression.Optimizer), the utilization of which would like like so:

```csharp
ActiveExpression.Optimizer = ExpressionOptimizer.tryVisit;

var a = Expression.Parameter(typeof(bool));
var b = Expression.Parameter(typeof(bool));

var lambda = Expression.Lambda<Func<bool, bool, bool>>
(
    Expression.AndAlso
    (
        Expression.Not(a),
        Expression.Not(b)
    ),
    a,
    b
); // lambda explicitly defined as (a, b) => !a && !b

var expr = ActiveExpression.Create<bool>(lambda, false, false);
// optimizer has intervened and defined expr as (a, b) => !(a || b)
// (because Augustus De Morgan said they're essentially the same thing, but this revision involves less steps)
```

## Collections

[![Cogs.Collections Nuget](https://img.shields.io/nuget/v/Cogs.Collections.svg)](https://www.nuget.org/packages/Cogs.Collections)

This library provides a number of utilities surrounding collections:

* `EquatableList<T>` is an immutable list of items which may be compared with other instances of the same type and produces a hash code based on the permutation of its contents.
* `INotifyGenericCollectionChanged<T>` is similar to the BCL's `INotifyCollectionChanged` except that it is a generic and therefore provides event arguments aware of the type of the collection.
* `ObservableDictionary<TKey, TValue>` and `ObservableSortedDictionary<TKey, TValue>` are counterparts to the BCL's `Dictionary<TKey, TValue>` and `SortedDictionary<TKey, TValue>`, respectively, that implement the also included `IRangeDictionary<TKey, TValue>` and `INotifyDictionaryChanged<TKey, TValue>`. Ever want to add multiple items to a dictionary at once... or keep an eye on what's being done to it? Now you can.

## Components

[![Cogs.Components Nuget](https://img.shields.io/nuget/v/Cogs.Components.svg)](https://www.nuget.org/packages/Cogs.Components)

For now, all this library offers is the `PropertyChangeNotifier` class, which you may inherit from to quickly get all the property utilities we're all tired of copying and pasting everywhere. Just call the protected `OnPropertyChanged` and `OnPropertyChanging` methods at the appropriate times from setters and compiler services will figure out what property you're in. Or, if all you need to do is set the value of a field, `SetBackedProperty` couldn't make it any easier or convenient to handle that as efficiently as possible.

## Disposal

[![Cogs.Disposal Nuget](https://img.shields.io/nuget/v/Cogs.Disposal.svg)](https://www.nuget.org/packages/Cogs.Disposal)

Much like the Components library, this library features base classes that handle things we've written a thousand times over, this time involving disposal. If you want to go with an implementation of the tried and true `IDisposable`, just inherit from `SyncDisposable`. Want a taste of the new `IAsyncDisposable`? Then, inherit from `AsyncDisposable`. Or, if you want to support both, there's `Disposable`. Each of these features abstract methods to actually do your disposal. But the base classes handle all of the following for you:

* proper use of `GC.SuppressFinalize`
* monitored access to disposal to ensure it can't happen twice
* the ability to override or "cancel" disposal by returning false from the abstract methods (e.g. you're reference counting and only want to dispose when your counter reaches zero)
* a protected `ThrowIfDisposed` method you can call to before doing anything that requires you haven't been disposed
* an `IsDisposed` property the value (and change notifications) of which are handled for you

This library provides the `IDisposalStatus` interface, which defines the `IsDisposed` property and all the base classes implement it.

Lastly, it provides the `INotifyDisposing`, `INotifyDisposed`, and `INotifyDisposalOverridden` interfaces, which add events that notify of these occurrences. If you're using the base classes in this library, you don't need to worry about unregistering handlers. The base classes drop all the references in the events' invocation lists on their own. We're not trying to *create* leaks here!

## Reflection

[![Cogs.Reflection Nuget](https://img.shields.io/nuget/v/Cogs.Reflection.svg)](https://www.nuget.org/packages/Cogs.Reflection)

## Synchronized Collections

[![Cogs.Collections.Synchronized Nuget](https://img.shields.io/nuget/v/Cogs.Collections.Synchronized.svg)](https://www.nuget.org/packages/Cogs.Collections.Synchronized)

## Threading

[![Cogs.Threading Nuget](https://img.shields.io/nuget/v/Cogs.Threading.svg)](https://www.nuget.org/packages/Cogs.Threading)


# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
