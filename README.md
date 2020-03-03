![Cogs Logo](Cogs.jpg) 

<h1>Cogs</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard2.1`.

- [Libraries](#libraries)
  - [Active Expressions](#active-expressions)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Libraries

## Active Expressions

[![Gear.ActiveExpressions Nuget](https://img.shields.io/nuget/v/Cogs.ActiveExpressions.svg)](https://www.nuget.org/packages/Cogs.ActiveExpressions)

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

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
