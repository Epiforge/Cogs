![Cogs Logo](Cogs.jpg) 

<h1>Cogs</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard2.1`.

![Azure Pipelines](https://dev.azure.com/epiforge/cogs/_apis/build/status/2)
![Build](https://img.shields.io/azure-devops/build/epiforge/cogs/2.svg?logo=microsoft&logoColor=white)
![Tests](https://img.shields.io/azure-devops/tests/epiforge/cogs/2.svg?compact_message=&logo=microsoft&logoColor=white)

- [Libraries](#libraries)
  - [Active Expressions](#active-expressions)
  - [Active Query](#active-query)
  - [Collections](#collections)
  - [Components](#components)
  - [Disposal](#disposal)
  - [Exceptions](#exceptions)
  - [Reflection](#reflection)
  - [Synchronized Collections](#synchronized-collections)
  - [Threading](#threading)
  - [Windows](#windows)
  - [Wpf](#wpf)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Libraries

## Active Expressions

[![Cogs.ActiveExpressions Nuget](https://img.shields.io/nuget/v/Cogs.ActiveExpressions.svg)](https://www.nuget.org/packages/Cogs.ActiveExpressions)

This library accepts a `LambdaExpression` and arguments to pass to it, dissects the `LambdaExpression`'s body, and hooks into change notification events for properties (`INotifyPropertyChanged`), collections (`INotifyCollectionChanged`), and dictionaries (`Cogs.Collections.INotifyDictionaryChanged`).

```csharp
// Employee implements INotifyPropertyChanged
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth);
// expr subscribed to elizabeth's PropertyChanged
```

Then, as changes involving any elements of the expression occur, a chain of automatic re-evaluation will get kicked off, possibly causing the active expression's `Value` property to change.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth);
// expr.Value == 9
elizabeth.Name = "Lizzy";
// expr.Value == 5
```

Also, since exceptions may be encountered after an active expression was created due to subsequent element changes, active expressions also have a `Fault` property, which will be set to the exception that was encountered during evaluation.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth);
// expr.Fault is null
elizabeth.Name = null;
// expr.Fault is NullReferenceException
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
// (because Augustus De Morgan said they're essentially the same thing, but this involves less steps)
```

## Active Query

[![Cogs.ActiveQuery Nuget](https://img.shields.io/nuget/v/Cogs.ActiveQuery.svg)](https://www.nuget.org/packages/Cogs.ActiveQuery)

This library provides re-implementations of extension methods you know and love from `System.Linq.Enumerable`, but instead of returning `Enumerable<T>`s and simple values, these return `ActiveEnumerable<T>`s, `ActiveDictionary<TKey, TValue>`s, and `ActiveValue<T>`s.
This is because, unlike traditional LINQ extension methods, these extension methods continuously update their results until those results are disposed.

But... what could cause those updates?

* the source is enumerable, implements `INotifyCollectionChanged`, and raises a `CollectionChanged` event
* the source is a dictionary, implements `Cogs.Collections.INotifyDictionaryChanged<TKey, TValue>`, and raises a `DictionaryChanged` event
* the elements in the enumerable (or the values in the dictionary) implement `INotifyPropertyChanged` and raise a `PropertyChanged` event
* a reference enclosed by a selector or a predicate passed to the extension method implements `INotifyCollectionChanged`, `Cogs.Collections.INotifyDictionaryChanged<TKey, TValue>`, or `INotifyPropertyChanged` and raises one of their events

That last one might be a little surprising, but this is because all selectors and predicates passed to Active Query extension methods become active expressions (see above).
This means that you will not be able to pass one that the Active Expressions library doesn't support (e.g. a lambda expression that can't be converted to an expression tree or that contains nodes that Active Expressions doesn't deal with).
But, in exchange for this, you get all kinds of notification plumbing that's just handled for you behind the scenes.

Suppose, for example, you're working on an app that displays a list of notes and you want the notes to be shown in descending order of when they were last edited.

```csharp
var notes = new ObservableCollection<Note>();

var orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
notesViewControl.ItemsSource = orderedNotes;
```

From then on, as you add `Note`s to the `notes` observable collection, the `ActiveEnumerable<Note>` named `orderedNotes` will be kept ordered so that `notesViewControl` displays them in the preferred order.

Since the `ActiveEnumerable<T>` is automatically subscribing to events for you, you do need to call `Dispose` on it when you don't need it any more.

```csharp
void Page_Unload(object sender, EventArgs e)
{
    orderedNotes.Dispose();
}
```

But, you may ask, what happens if things are a little bit more complicated because of background work?
Suppose...

```csharp
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
});
```

Since we called the `Cogs.Collections.Synchronized.SynchronizedObservableCollection` constructor in the context of a TPL `Task` and without specifying a `SynchronizationContext`, operations performed on it will not be in the context of our UI thread.
Manipulating this collection on a background thread might be desirable, but there will be a big problem if we bind a UI control to it, since non-UI threads shouldn't be messing with UI controls.
For this specific reason, Active Query offers a special extension method that will perform the final operations on an enumerable (or dictionary) using a specific `SynchronizationContext`.

```csharp
var uiContext = SynchronizationContext.Current;
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
ActiveEnumerable<Note> notesForBinding;
Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
    notesForBinding = orderedNotes.SwitchContext(uiContext);
});
```

Or, if you call `SwitchContext` without any arguments but when you know you're already running in the UI's context, it will assume you want to switch to that.

```csharp
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
await Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
});
var notesForBinding = orderedNotes.SwitchContext();
```

But, keep in mind that no Active Query extension methods mutate the objects for which they are called, which means now you have two things to dispose, and in the right order!

```csharp
void Page_Unload(object sender, EventArgs e)
{
    notesForBinding.Dispose();
    orderedNotes.Dispose();
}
```

Ahh, but what about exceptions?
Well, active expressions expose a `Fault` property and raise `PropertyChanging` and `PropertyChanged` events for it, but... you don't really see those active expressions as an Active Query caller, do ya?
For that reason, Active Query introduces the `INotifyElementFaultChanges` interface, which is implemented by `ActiveEnumerable<T>`, `ActiveDictionary<TKey, TValue>`, and `ActiveValue<T>`.
You may subscribe to its `ElementFaultChanging` and `ElementFaultChanged` events to be notified when an active expression runs into a problem.
You may also call the `GetElementFaults` method at any time to retrieve a list of the elements (or key/value pairs) that have active expressions that are currently faulted and what the exception was in each case.

As with the Active Expressions library, you can use the static property `Optimizer` to specify an optimization method to invoke automatically during the active expression creation process.
However, please note that Active Query also has its own version of this property on the `ActiveQueryOptions` static class.
If you are not using Active Expressions directly, we recommend using Active Query's property instead because the optimizer will be called only once per extension method call in that case, no matter how many elements or key/value pairs are processed by it.
Optimize your optimization, yo.

## Collections

[![Cogs.Collections Nuget](https://img.shields.io/nuget/v/Cogs.Collections.svg)](https://www.nuget.org/packages/Cogs.Collections)

This library provides a number of utilities surrounding collections:

* `EquatableList<T>` is an immutable list of items which may be compared with other instances of the same type and produces a hash code based on the permutation of its contents.
* `NullableKeyDictionary<TKey, TValue>` and `NullableKeySortedDictionary<TKey, TValue>` are very slim implementations of `IDictionary<TKey, TValue>` that allow a single null key (useful for some edge cases in which a null key is simply going to happen and you need to be able to deal with it; otherwise, use other dictionary classes)
* `ObservableDictionary<TKey, TValue>` and `ObservableSortedDictionary<TKey, TValue>` are counterparts to the BCL's `Dictionary<TKey, TValue>` and `SortedDictionary<TKey, TValue>`, respectively, that implement the also included `IRangeDictionary<TKey, TValue>` and `INotifyDictionaryChanged<TKey, TValue>`. Ever want to add multiple items to a dictionary at once... or keep an eye on what's being done to it? Now you can.
* `OrderedHashSet<T>` is a counterpart to the BCL's `HashSet<T>` that maintains the order of the elements in the set. All operations are still *O(1)*, just like the original, but if you enumerate over it you will get elements in the exact order they were added. There are also methods for manipulating the order.

## Components

[![Cogs.Components Nuget](https://img.shields.io/nuget/v/Cogs.Components.svg)](https://www.nuget.org/packages/Cogs.Components)

This library offers the `PropertyChangeNotifier` class, which you may inherit from to quickly get all the property utilities we're all tired of copying and pasting everywhere.
Just call the protected `OnPropertyChanged` and `OnPropertyChanging` methods at the appropriate times from setters and compiler services will figure out what property you're in.
Or, if all you need to do is set the value of a field, `SetBackedProperty` couldn't make it any easier or convenient to handle that as efficiently as possible.
`DynamicPropertyChangeNotifier` is also available if your class needs to be dynamic.

## Disposal

[![Cogs.Disposal Nuget](https://img.shields.io/nuget/v/Cogs.Disposal.svg)](https://www.nuget.org/packages/Cogs.Disposal)

Much like the Components library, this library features base classes that handle things we've written a thousand times over, this time involving disposal.
If you want to go with an implementation of the tried and true `IDisposable`, just inherit from `SyncDisposable`. Want a taste of the new `IAsyncDisposable`?
Then, inherit from `AsyncDisposable`.
Or, if you want to support both, there's `Disposable`.
Each of these features abstract methods to actually do your disposal.
But all of the base classes feature:

* proper implementation of the finalizer and use of `GC.SuppressFinalize`
* monitored access to disposal to ensure it can't happen twice
* the ability to override or "cancel" disposal by returning false from the abstract methods (e.g. you're reference counting and only want to dispose when your counter reaches zero)
* a protected `ThrowIfDisposed` method you can call to before doing anything that requires you haven't been disposed
* an `IsDisposed` property the value (and change notifications) of which are handled for you

This library provides the `IDisposalStatus` interface, which defines the `IsDisposed` property and all the base classes implement it. This library also provides the `INotifyDisposing`, `INotifyDisposed`, and `INotifyDisposalOverridden` interfaces, which add events that notify of these occurrences.

Lastly, this library provides `DisposableValuesCache` and `AsyncDisposableValuesCache`, which each represents a cache of key-value pairs which, once disposed by all retrievers, are removed.

## Exceptions

[![Cogs.Exceptions Nuget](https://img.shields.io/nuget/v/Cogs.Exceptions.svg)](https://www.nuget.org/packages/Cogs.Exceptions)

This library provides extension methods for dealing with exceptions:

* `GetFullDetails` - creates a representation of an exception and all of its inner exceptions, including exception types, messages, and stack traces, and traversing multiple inner exceptions in the case of `AggregateException`

## Reflection

[![Cogs.Reflection Nuget](https://img.shields.io/nuget/v/Cogs.Reflection.svg)](https://www.nuget.org/packages/Cogs.Reflection)

This library has useful tools for when you can't be certain of some things at compile time, such as types, methods, etc.
While .NET reflection is immensely powerful, it's not very quick.
To address this, this library offers the following classes:

* `FastComparer` - provides a method for comparing instances of a type that is not known at compile time
* `FastConstructorInfo` - provides a method for invoking a constructor that is not known at compile time
* `FastDefault` - provides a method for getting the default value of a type that is not known at compile time
* `FastEqualityComparer` - provides methods for testing equality of and getting hash codes for instances of a type that is not known at compile time
* `FastMethodInfo` - provides a method for invoking a method that is not known at compile time

All of the above classes use reflection to initialize utilities for types at runtime, however they create delegates to perform at much better speeds and cache instances of themselves to avoid having to perform the same reflection twice.
And yes, the caching is thread-safe.

Also includes extension methods for `Type` which search for implementations of events, methods, and properties.
Also includes `GenericOperations` which provides methods for adding, dividing, multiplying, and/or subtracting objects.

## Synchronized Collections

[![Cogs.Collections.Synchronized Nuget](https://img.shields.io/nuget/v/Cogs.Collections.Synchronized.svg)](https://www.nuget.org/packages/Cogs.Collections.Synchronized)

Good idea: binding UI elements to observable collections.
Bad idea: manipulating observable collections bound to UI elements from background threads.
Why?
Because the collection change notification event handlers will be executed on non-UI threads, which cannot safely manipulate the UI.
So, I guess we need to carefully marshal calls over to the UI thread whenever we manipulate or even read those observable collections, right?

Not anymore.

Introducing the `SynchronizedObservableCollection<T>`, `SynchronizedObservableDictionary<TKey, TValue>`, and `SynchronizedObservableSortedDictionary<TKey, TValue>` classes.
Create them on UI threads.
Or, pass the UI thread's synchronization context to their constructors.
Then, any time they are touched, the call is marshalled to the context of the appropriate thread.
They even include async alternatives to every method and indexer just in case you would like to be well-behaved and not block worker threads just because the UI thread is busy.

I mean, no judgment.
We just don't like sending threads to thread jail.

Last, but not least, each of them also has an array of range methods to handle performing multiple operations at once when you know you'll need to in advanced and would like to avoid O(2n) context switching.

## Threading

[![Cogs.Threading Nuget](https://img.shields.io/nuget/v/Cogs.Threading.svg)](https://www.nuget.org/packages/Cogs.Threading)

This is where we keep all our utilities for multi-threaded stuff.

* `AsyncExtensions` - provides extensions for dealing with async utilities like `TaskCompletionSource<TResult>`
* `AsyncSynchronizationContext` - provides a synchronization context for the Task Parallel Library
* `ISynchronized` - represents an object the operations of which occur on a specific synchronization context (used extensively by the Synchronized Collections library, above)
* `SynchronizedExtensions` - provides extensions for executing operations with instances of `System.Threading.SynchronizationContext` and `ISynchronized`

## Windows

[![Cogs.Windows Nuget](https://img.shields.io/nuget/v/Cogs.Windows.svg)](https://www.nuget.org/packages/Cogs.Windows)

This library includes utilities for interoperation with Microsoft Windows, including:

* `Activation` - provides information relating to Windows Activation
* `ConsoleAssist` - provides methods for interacting with consoles
* `Cursor` - wraps Win32 API methods dealing with the cursor
* `Shell` - wraps methods of the WScript.Shell COM object (specifically useful for invoking its `CreateShortcut` function)
* `Theme` - represents the current Windows theme

Also provides extension methods for dealing with processes, including:

* `CloseMainWindowAsync` - close the main window of the specified process
* `GetParentProcess` - gets the parent process of the specified process

## Wpf

[![Cogs.Wpf Nuget](https://img.shields.io/nuget/v/Cogs.Wpf.svg)](https://www.nuget.org/packages/Cogs.Wpf)

This library includes utilities for Windows Presentation Foundation, including:

* `ActionCommand` - a command that can be manipulated by its caller
* `ControlAssist` - provides attached dependency properties to enhance the functionality of controls (e.g. `AdditionalInputBindings`)
* `Screen` - represents a display device or multiple display devices on a single system
* `WindowAssist` - provides attached dependency properties to enhance the functionality of windows (e.g. `AutoActivation`, `BlurBehind`, `IsBlurredBehind`, `IsCaption`, `SendSystemCommand`, `SetDefaultWindowStyleOnSystemCommands`, `ShowSystemMenu`)

Also includes extension methods for visuals:

* `GetVisualAncestor` - gets the first ancestor of a reference in the Visual Tree, or <c>null</c> if none could be found
* `GetVisualDescendent` - gets the first member of a Visual Tree descending from a reference, or <c>null</c> if none could be found

Also includes extension methods for windows:

* `IsInSafePosition` - gets whether the specified window is completely contained within the closest working area
* `SafeguardPosition` - moves the specified window the minimum amount to be completely contained within the closest working area

Also includes behaviors:

* `ComboBoxDataVirtualization` & `ListBoxDataVirtualization` - sets the items source of a combo box or list box (including list views), respectively, to a collection that loads elements as they are needed for display and keeps selected elements loaded (requires .NET Core 3.1 or later)
* `DelayedFocus` - focuses an element after a specified delay
* `DeselectAllOnEmptySpaceClicked` - feselects all items when empty space in a list view is clicked
* `OpenNavigateUri` - opens the `Hyperlink`'s `NavigateUri` when it is clicked
* `PasswordBindingTarget` - allows binding to `PasswordBox.Password`

Also includes controls:

* `UrlAwareTextBlock` - provides a lightweight control for displaying small amounts of flow content which finds URLs and makes them clickable hyperlinks

Also includes input gestures:

* `MouseWheelDownGesture` - defines a mouse wheel down gesture that can be used to invoke a command
* `MouseWheelUpGesture` - defines a mouse wheel up gesture that can be used to invoke a command

Also includes validation rules:

* `InvalidCharactersValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid characters
* `StringNotEmptyValidationRule` - provides a way to create a rule in order to check that user input is not an empty string
* `ValidFileNameValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid file name characters
* `ValidPathValidationRule` - provides a way to create a rule in order to check that user input does not contain any invalid file system path characters

Also includes a wide array of value converters. Please see a package explorer for details.

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
