Much like the Components library, this library features base classes that handle things we've written a thousand times over, this time involving disposal. If you want to go with an implementation of the tried and true `IDisposable`, just inherit from `SyncDisposable`. Want a taste of the new `IAsyncDisposable`? Then, inherit from `AsyncDisposable`. Or, if you want to support both, there's `Disposable`. Each of these features abstract methods to actually do your disposal. But all of the base classes feature:

* proper implementation of the finalizer and use of `GC.SuppressFinalize`
* monitored access to disposal to ensure it can't happen twice
* the ability to override or "cancel" disposal by returning false from the abstract methods (e.g. you're reference counting and only want to dispose when your counter reaches zero)
* a protected `ThrowIfDisposed` method you can call to before doing anything that requires you haven't been disposed
* an `IsDisposed` property the value (and change notifications) of which are handled for you

This library provides the `IDisposalStatus` interface, which defines the `IsDisposed` property and all the base classes implement it.

Lastly, it provides the `INotifyDisposing`, `INotifyDisposed`, and `INotifyDisposalOverridden` interfaces, which add events that notify of these occurrences.