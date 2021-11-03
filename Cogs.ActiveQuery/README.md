This library provides re-implementations of extension methods you know and love from `System.Linq.Enumerable`, but instead of returning `Enumerable<T>`s and simple values, these return `ActiveEnumerable<T>`s, `ActiveDictionary<TKey, TValue>`s, and `ActiveValue<T>`s. This is because, unlike traditional LINQ extension methods, these extension methods continuously update their results until those results are disposed.

But... what could cause those updates?

* the source is enumerable, implements `INotifyCollectionChanged`, and raises a `CollectionChanged` event
* the source is a dictionary, implements `Cogs.Collections.INotifyDictionaryChanged<TKey, TValue>`, and raises a `DictionaryChanged` event
* the elements in the enumerable (or the values in the dictionary) implement `INotifyPropertyChanged` and raise a `PropertyChanged` event
* a reference enclosed by a selector or a predicate passed to the extension method implements `INotifyCollectionChanged`, `Cogs.Collections.INotifyDictionaryChanged<TKey, TValue>`, or `INotifyPropertyChanged` and raises one of their events

That last one might be a little surprising, but this is because all selectors and predicates passed to Active Query extension methods become active expressions (see above). This means that you will not be able to pass one that the Active Expressions library doesn't support (e.g. a lambda expression that can't be converted to an expression tree or that contains nodes that Active Expressions doesn't deal with). But, in exchange for this, you get all kinds of notification plumbing that's just handled for you behind the scenes.

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

But, you may ask, what happens if things are a little bit more complicated because of background work? Suppose...

```csharp
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
});
```

Since we called the `Cogs.Collections.Synchronized.SynchronizedObservableCollection` constructor in the context of a TPL `Task` and without specifying a `SynchronizationContext`, operations performed on it will not be in the context of our UI thread. Manipulating this collection on a background thread might be desirable, but there will be a big problem if we bind a UI control to it, since non-UI threads shouldn't be messing with UI controls. For this specific reason, Active Query offers a special extension method that will perform the final operations on an enumerable (or dictionary) using a specific `SynchronizationContext`.

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

Ahh, but what about exceptions? Well, active expressions expose a `Fault` property and raise `PropertyChanging` and `PropertyChanged` events for it, but... you don't really see those active expressions as an Active Query caller, do ya? For that reason, Active Query introduces the `INotifyElementFaultChanges` interface, which is implemented by `ActiveEnumerable<T>`, `ActiveDictionary<TKey, TValue>`, and `ActiveValue<T>`. You may subscribe to its `ElementFaultChanging` and `ElementFaultChanged` events to be notified when an active expression runs into a problem. You may also call the `GetElementFaults` method at any time to retrieve a list of the elements (or key/value pairs) that have active expressions that are currently faulted and what the exception was in each case.

As with the Active Expressions library, you can use the static property `Optimizer` to specify an optimization method to invoke automatically during the active expression creation process. However, please note that Active Query also has its own version of this property on the `ActiveQueryOptions` static class. If you are not using Active Expressions directly, we recommend using Active Query's property instead because the optimizer will be called only once per extension method call in that case, no matter how many elements or key/value pairs are processed by it. Optimize your optimization, yo.