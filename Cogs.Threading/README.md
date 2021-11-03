This is where we keep all our utilities for multi-threaded stuff.

* `AsyncExtensions` - provides extensions for dealing with async utilities like `TaskCompletionSource<TResult>`
* `AsyncSynchronizationContext` - provides a synchronization context for the Task Parallel Library
* `ISynchronized` - represents an object the operations of which occur on a specific synchronization context (used extensively by the Synchronized Collections library, above)
* `SynchronizedExtensions` - provides extensions for executing operations with instances of `System.Threading.SynchronizationContext` and `ISynchronized`