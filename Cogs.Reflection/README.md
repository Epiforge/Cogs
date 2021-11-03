This library has useful tools for when you can't be certain of some things at compile time, such as types, methods, etc. While .NET reflection is immensely powerful, it's not very quick. To address this, this library offers the following classes:

* `FastComparer` - provides a method for comparing instances of a type that is not known at compile time
* `FastConstructorInfo` - provides a method for invoking a constructor that is not known at compile time
* `FastDefault` - provides a method for getting the default value of a type that is not known at compile time
* `FastEqualityComparer` - provides methods for testing equality of and getting hash codes for instances of a type that is not known at compile time
* `FastMethodInfo` - provides a method for invoking a method that is not known at compile time

All of the above classes use reflection to initialize utilities for types at runtime, however they create delegates to perform at much better speeds and cache instances of themselves to avoid having to perform the same reflection twice. And yes, the caching is thread-safe.

Also includes extension methods for `Type` which search for implementations of events, methods, and properties. Also includes `GenericOperations` which provides methods for adding, dividing, multiplying, and/or subtracting objects.