using System;
using System.Collections.Generic;

namespace Gear.ActiveQuery
{
    internal static class ExceptionHelper
    {
        public static ArgumentOutOfRangeException IndexArgumentWasOutOfRange => new ArgumentOutOfRangeException("index", "Index was out of range. Must be non-negative and less than the size of the collection.");

        public static KeyNotFoundException KeyNotFound => new KeyNotFoundException();

        public static ArgumentNullException KeyNull => new ArgumentNullException("key");

        public static ArgumentException SameKeyAlreadyAdded => new ArgumentException("An item with the same key has already been added.");

        public static InvalidOperationException SequenceContainsMoreThanOneElement => new InvalidOperationException("Sequence contains more than one element");

        public static InvalidOperationException SequenceContainsNoElements => new InvalidOperationException("Sequence contains no elements");
    }
}
