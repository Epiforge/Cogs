This library provides extension methods for dealing with exceptions:

* `GetFullDetails` - creates a representation of an exception and all of its inner exceptions, including exception types, messages, and stack traces, and traversing multiple inner exceptions in the case of `AggregateException`