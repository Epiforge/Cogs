Good idea: binding UI elements to observable collections. Bad idea: manipulating observable collections bound to UI elements from background threads. Why? Because the collection change notification event handlers will be executed on non-UI threads, which cannot safely manipulate the UI. So, I guess we need to carefully marshal calls over to the UI thread whenever we manipulate or even read those observable collections, right?

Not anymore.

Introducing the `SynchronizedObservableCollection<T>`, `SynchronizedObservableDictionary<TKey, TValue>`, and `SynchronizedObservableSortedDictionary<TKey, TValue>` classes. Create them on UI threads. Or, pass the UI thread's synchronization context to their constructors. Then, any time they are touched, the call is marshalled to the context of the appropriate thread. They even include async alternatives to every method and indexer just in case you would like to be well-behaved and not block worker threads just because the UI thread is busy.

I mean, no judgment. We just don't like sending threads to thread jail.

Last, but not least, each of them also has an array of range methods to handle performing multiple operations at once when you know you'll need to in advanced and would like to avoid O(2n) context switching.