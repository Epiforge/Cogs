namespace Cogs.ActiveQuery;

static class ExceptionHelper
{
    public static ArgumentOutOfRangeException IndexArgumentWasOutOfRange =>
        new("index", "Index was out of range. Must be non-negative and less than the size of the collection.");

    public static KeyNotFoundException KeyNotFound =>
        new();

    public static ArgumentNullException KeyNull =>
        new("key");

    public static ArgumentException SameKeyAlreadyAdded =>
        new("An item with the same key has already been added.");

    public static InvalidOperationException SequenceContainsMoreThanOneElement =>
        new("Sequence contains more than one element");

    public static InvalidOperationException SequenceContainsNoElements =>
        new("Sequence contains no elements");
}
